﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace dd3
{
    public class dd3Server
    {
        private readonly static Lazy<dd3Server> _instance = new Lazy<dd3Server>(
            () => new dd3Server(GlobalHost.ConnectionManager.GetHubContext<dd3Hub>().Clients));

        private static List<ConcurrentDictionary<string, BrowserInfo>> browserList = new List<ConcurrentDictionary<string, BrowserInfo>>();
        private static int currentSession = 0;

        private readonly object _locker = new Object();
        private Timer _timerToStart;
        private readonly TimeSpan _timeLimitConnect = TimeSpan.FromSeconds(10);

        private dd3Server(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
        }

        public static dd3Server Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public IHubConnectionContext<dynamic> Clients { get; set; }

        public void newClient(string cid, BrowserInfo b)
        {
            lock (_locker)
            {
                ConcurrentDictionary<string, BrowserInfo> list;

                if (_timerToStart != null)
                {
                    _timerToStart.Dispose();
                }

                if (browserList.Count - 1 < currentSession)
                {
                    list = new ConcurrentDictionary<string, BrowserInfo>();
                    browserList.Add(list);
                }
                else
                {
                    list = browserList[currentSession];
                }

                list.TryAdd(cid, new BrowserInfo(cid, b.browserNum, b.peerId, b.col, b.row, b.height, b.width));

                _timerToStart = new Timer(broadcastConfiguration, null, _timeLimitConnect, _timeLimitConnect);
            }
        }

        private void broadcastConfiguration (Object state) {
            _timerToStart.Dispose();
            currentSession++;
            ConcurrentDictionary<string, BrowserInfo> list = browserList[currentSession-1];
            BrowserBroadcastInfo[] browserInfos = new BrowserBroadcastInfo[list.Count];
            int i = 0;

            foreach (var item in list.Values)
            {
                browserInfos[i] = new BrowserBroadcastInfo(item.browserNum, item.peerId, item.col, item.row);
                i++;
            }

            String browserInfoJson = Newtonsoft.Json.JsonConvert.SerializeObject(browserInfos);
            Clients.All.receiveConfiguration(browserInfoJson);

        }


    }

    public class BrowserBroadcastInfo
    {
        public BrowserBroadcastInfo(string browserNum, string peerId, string col, string row)
        {
            this.browserNum = browserNum;
            this.peerId = peerId;
            this.col = col;
            this.row = row;
        }

        public string browserNum { get; set; }
        public string peerId { get; set; }
        public string col { get; set; }
        public string row { get; set; }
    }

    public class BrowserInfo
    {
        public BrowserInfo(string connectionId, string browserNum, string peerId, string col, string row, string height, string width)
        {
            this.connectionId = connectionId;
            this.browserNum = browserNum;
            this.peerId = peerId;
            this.col = col;
            this.row = row;
            this.height = height;
            this.width = width;
        }

        public string connectionId { get; set; }
        public string browserNum { get; set; }
        public string peerId { get; set; }
        public string col { get; set; }
        public string row { get; set; }
        public string height { get; set; }
        public string width { get; set; }
    }
}
