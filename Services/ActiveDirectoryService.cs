﻿using Serilog;
using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
     /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private ILogger _logger = Log.Logger;
        private Configuration _configuration = Configuration.Current;

        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public bool VerifyCredential(string userName, string password)
        {
            try
            {
                _logger.Debug($"Verifying user {userName} credential and status at {_configuration.Domain}");

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.Credential = new NetworkCredential(userName, password);
                    connection.Bind();
                }

                _logger.Information($"User {userName} credential and status verified successfully at {_configuration.Domain}");

                return true; //OK
            }
            catch (LdapException lex)
            {
                if (lex.ServerErrorMessage != null)
                {
                    var dataReason = ExtractErrorReason(lex.ServerErrorMessage);
                    if (dataReason != null)
                    {
                        _logger.Warning($"Verification user {userName} at {_configuration.Domain} failed: {dataReason}");
                        return false;
                    }
                }

                _logger.Error(lex, $"Verification user {userName} at {_configuration.Domain} failed");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user {userName} at {_configuration.Domain} failed");
            }

            return false;
        }

        private string ExtractErrorReason(string errorMessage)
        {
            var pattern = @"data ([0-9a-e]{3})";
            var match = Regex.Match(errorMessage, pattern);

            if (match.Success && match.Groups.Count == 2)
            {
                var data = match.Groups[1].Value;

                switch (data)
                {
                    case "525":
                        return "user not found";
                    case "52e":
                        return "invalid credentials";
                    case "530":
                        return "not permitted to logon at this time​";
                    case "531":
                        return "not permitted to logon at this workstation​";
                    case "532":
                        return "password expired";
                    case "533":
                        return "account disabled";
                    case "701":
                        return "account expired";
                    case "773":
                        return "user must change password";
                    case "775":
                        return "user account locked";
                }
            }

            return null;
        }
    }
}