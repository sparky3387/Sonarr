﻿using System;
using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrentSettingsValidator : AbstractValidator<RTorrentSettings>
    {
        public RTorrentSettingsValidator()
        {
            RuleFor(c => c.Host).NotEmpty();
            RuleFor(c => c.Port).InclusiveBetween(0, 65535);
            RuleFor(c => c.TvCategory).NotEmpty();
        }
    }

    public class RTorrentSettings : IProviderConfig
    {
        private static readonly RTorrentSettingsValidator Validator = new RTorrentSettingsValidator();

        public RTorrentSettings()
        {
            Host = "localhost";
            Port = 8080;
            UrlBase = "RPC2";
            TvCategory = "tv-sonarr";
            OlderTvPriority = (int)RTorrentPriority.Normal;
            RecentTvPriority = (int)RTorrentPriority.Normal;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "UrlBase", Type = FieldType.Textbox)]
        public string UrlBase { get; set; }

        [FieldDefinition(3, Label = "Use SSL", Type = FieldType.Checkbox)]
        public bool UseSsl { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Sonarr avoids conflicts with unrelated downloads, but it's optional.")]
        public string TvCategory { get; set; }

        [FieldDefinition(7, Label = "Recent Priority", Type = FieldType.Select, SelectOptions = typeof(RTorrentPriority), HelpText = "Priority to use when grabbing episodes that aired within the last 14 days")]
        public int RecentTvPriority { get; set; }

        [FieldDefinition(8, Label = "Older Priority", Type = FieldType.Select, SelectOptions = typeof(RTorrentPriority), HelpText = "Priority to use when grabbing episodes that aired over 14 days ago")]
        public int OlderTvPriority { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
