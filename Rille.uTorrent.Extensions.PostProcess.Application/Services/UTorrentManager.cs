using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;
using RestSharp.Authenticators;
using Rille.uTorrent.Extensions.PostProcess.Model;
using System.Net;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    /// <summary>
    /// http://help.utorrent.com/customer/en/portal/topics/664593-web-api-and-webui-/articles
    /// 
    /// http://help.utorrent.com/customer/en/portal/articles/1573947-torrent-labels-list---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573951-torrent-job-properties---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573949-files-list---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573952-actions---webapi
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    public class UTorrentManager : ITorrentManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Config _config;
        private readonly RestClient _restClient;
        private FileManager fileManager;

        public UTorrentManager(Config config)
        {
            _config = config;
            _restClient = new RestClient(_config.TorrentWebApiUrl);
            _restClient.Authenticator = new HttpBasicAuthenticator(_config.TorrentWebApiLogin, _config.TorrentWebApiPassword);
            //_restClient.Authenticator = new SimpleAuthenticator("username", "admin", "password", "admin");
        }

        public UTorrentManager(Config config, FileManager fileManager) : this(config)
        {
            this.fileManager = fileManager;
        }

        public List<Torrent> GetTorrentList()
        {
            var ret = new List<Torrent>();
            var req = new RestRequest("gui/?list=1");

            //var resp = _restClient.Get<dynamic>(req);
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

            if (string.IsNullOrEmpty(response.Content))
                return new List<Torrent>();

            dynamic json = JObject.Parse(response.Content);
            var torrentsJArray = (JArray)json.torrents;
            foreach (var jToken in torrentsJArray)
            {
                var torrent = new Torrent(jToken[0].ToString(), _config);
                torrent.TorrentStatus = (TorrentStatus)(int)jToken[1];
                torrent.Name = jToken[2].ToString();
                torrent.ProgressPercent = (int)jToken[4] / 10;
                torrent.Path = jToken[26].ToString();
                torrent.ActualSeedRatioPercent = (int)jToken[7] / 10;
                torrent.Label = (string)jToken[11];

                if (torrent.Label == "")
                    torrent.ProcessingStatus = ProcessingStatus.None;
                else
                {
                    Enum.TryParse(torrent.Label, true, out ProcessingStatus outEnum);
                    torrent.ProcessingStatus = outEnum;
                }
                    

                PopulateTorrentTrackers(torrent);
                PopulateTorrentFileList(torrent);
                MatchSeedingGoalsWithTracker(torrent);

                ret.Add(torrent);
            }
            return ret;
        }

        public void Start(Torrent torrent)
        {
            var req = new RestRequest($"gui/?action=start&hash={torrent.Hash}");
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi when starting Torrent. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");
        }


        public void DeleteTorrent(Torrent torrent)
        {
            var req = new RestRequest($"gui/?action=removedata&hash={torrent.Hash}");
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

        }

        public bool HasTorrentGoalsBeenReached(Torrent torrent)
        {
            bool ratioReached = torrent.ActualSeedRatioPercent >= torrent.SeedingGoal.SeedRatioPercent;
            return ratioReached;
        }

        public bool HasTorrentBeenProcessed(Torrent torrent)
        {
            return torrent.ProcessingStatus == ProcessingStatus.Processed;
        }

        public void MarkTorrentAsProcessing(Torrent torrent)
        {
            SetLabelOnTorrent(torrent, ProcessingStatus.Processing);
        }

        public void MarkTorrentAsProcessed(Torrent torrent)
        {
            SetLabelOnTorrent(torrent, ProcessingStatus.Processed);
        }

        public void MarkTorrentAsProcessFailed(Torrent torrent)
        {
            SetLabelOnTorrent(torrent, ProcessingStatus.ProcessFailed);
        }

        // Private helpers

        private void PopulateTorrentTrackers(Torrent torrent)
        {
            var req = new RestRequest("gui/?action=getprops&hash=" + torrent.Hash);
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

            if (string.IsNullOrEmpty(response.Content))
                throw new NullReferenceException("Got no content when asking the api for torrent properties!");

            dynamic json = JObject.Parse(response.Content);
            var tracker = (string)json.props[0].trackers;
            torrent.Trackers = tracker;


        }

        private void PopulateTorrentFileList(Torrent torrent)
        {
            var req = new RestRequest("gui/?action=getfiles&hash=" + torrent.Hash);
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

            if (string.IsNullOrEmpty(response.Content))
                throw new NullReferenceException("Got no content when asking the api for torrent files!");

            dynamic json = JObject.Parse(response.Content);
            torrent.FileList = new List<string>();
            var files = (JArray)json.files[1];
            var fileCount = files.Count;

            foreach (var file in files)
            {
                torrent.FileList.Add((string)file[0]);
            }
        }

        private void SetLabelOnTorrent(Torrent torrent, ProcessingStatus status)
        {
            // gui/?action=setprops&hash=[TORRENT HASH]&s=[PROPERTY]&v=[VALUE]
            var emptyLabelReq = new RestRequest($"gui/?action=setprops&hash={torrent.Hash}&s=label&v=");
            var setNewLabelReq = new RestRequest($"gui/?action=setprops&hash={torrent.Hash}&s=label&v={status.ToString()}");

            _restClient.Execute(emptyLabelReq);
            var response = _restClient.Execute(setNewLabelReq);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

            torrent.ProcessingStatus = status;

        }

        private void MatchSeedingGoalsWithTracker(Torrent torrent)
        {
            foreach (var goal in _config.SeedingGoals)
            {
                if (Regex.IsMatch(torrent.Trackers, goal.TrackerRegex))
                {
                    torrent.SeedingGoal = goal;
                    return;
                }
            }

            // If no match, goal becomes default
            torrent.SeedingGoal = _config.SeedingGoals.First(p => p.TrackerRegex == "DEFAULT");
        }

        private bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 299);
        }

    }
}