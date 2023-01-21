﻿using System;

namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public class ApplicationCacheConfig
    {
        public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromSeconds(60);
    }
}