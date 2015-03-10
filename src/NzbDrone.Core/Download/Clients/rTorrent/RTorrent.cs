using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NLog;
using NzbDrone.Core.Validation;
using FluentValidation.Results;
using System.Net;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrent : TorrentClientBase<RTorrentSettings>
    {
        private readonly IRTorrentProxy _proxy;

        public RTorrent(IRTorrentProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            _proxy.AddTorrentFromUrl(magnetLink, Settings);

            // Wait until url has been resolved before returning
            var TRIES = 5;
            var remainingTries = TRIES;
            var RETRY_DELAY = 500; //ms
            var ready = false;
            while (remainingTries-- > 0)
            {
                ready = _proxy.HasHashTorrent(hash, Settings);

                if (ready)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(RETRY_DELAY);
                }
            }
           
            if (ready)
            {
                _proxy.SetTorrentLabel(hash, Settings.TvCategory, Settings);

                var priority = (RTorrentPriority)(remoteEpisode.IsRecentEpisode() ?
                                        Settings.RecentTvPriority : Settings.OlderTvPriority);
                _proxy.SetTorrentPriority(hash, Settings, priority);

                return hash;
            }
            else
            {
                _logger.Debug("Magnet {0} could not be resolved in {1} tries at {2} ms intervals.", magnetLink, TRIES, RETRY_DELAY);
                // Remove from client, since it is discarded
                RemoveItem(hash, false);

                return null;
            }
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromFile(filename, fileContent, Settings);

            _proxy.SetTorrentLabel(hash, Settings.TvCategory, Settings);

            var priority = (RTorrentPriority)(remoteEpisode.IsRecentEpisode() ?
                                    Settings.RecentTvPriority : Settings.OlderTvPriority);
            _proxy.SetTorrentPriority(hash, Settings, priority);

            return hash;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            try
            {
                var torrents = _proxy.GetTorrents(Settings);

                var items = new List<DownloadClientItem>();
                foreach (RTorrentTorrent torrent in torrents)
                {
                    // Don't concern ourselves with categories other than specified
                    if (torrent.Category != Settings.TvCategory) continue;

                    var item = new DownloadClientItem();
                    item.DownloadClient = Definition.Name;
                    item.Title = torrent.Name;
                    item.DownloadId = torrent.Hash;
                    item.OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(torrent.BaseDir));
                    item.TotalSize = torrent.TotalSize;
                    item.RemainingSize = torrent.RemainingSize;
                    item.Category = torrent.Category;

                    if (torrent.DownRate > 0) {
                        var secondsLeft = torrent.RemainingSize / torrent.DownRate;
                        item.RemainingTime = TimeSpan.FromSeconds(secondsLeft);
                    }

                    if (torrent.IsFinished) item.Status = DownloadItemStatus.Completed;
                    else if (torrent.IsActive) item.Status = DownloadItemStatus.Downloading;
                    else if (!torrent.IsActive) item.Status = DownloadItemStatus.Paused;
                    else if (!torrent.IsOpen) item.Status = DownloadItemStatus.Queued;

                    if (item.Status != DownloadItemStatus.Completed) item.IsReadOnly = true;
                    // FIXME Set conditions for  isreadonly = false 
                    else item.IsReadOnly = true;

                    items.Add(item);
                }

                return items;
            }
            catch (DownloadClientException ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return Enumerable.Empty<DownloadClientItem>();
            }

        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            _proxy.RemoveTorrent(downloadId, Settings);


            if (deleteData)
            {
                _logger.Info("RTorrent cannot remove data");
                /* TODO Not yet implemented */
            }
        }

        public override DownloadClientStatus GetStatus()
        {
            // FIXME
            var status = new DownloadClientStatus
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost"
            };

            return status;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.Any()) return;
            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("0.9.0"))
                {
                    return new ValidationFailure(string.Empty, "RTorrent version should be at least 0.9.0; version reported: " + version);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
