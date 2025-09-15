using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Net;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class LdapConnectionFactory
    {
        private readonly ILogger _logger;

        public LdapConnectionFactory(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates new connection to a ldap domain, binds as a current process user credential using Negotiate auth type and returns it.
        /// </summary>
        /// <param name="domain">LDAP domain</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LdapConnection CreateAsCurrentProcessUser(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException($"'{nameof(domain)}' cannot be null or whitespace.", nameof(domain));
            }

            _logger.Debug("Start connection to {Domain}", domain);
            var connection = new LdapConnection(domain);
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.RootDseCache = true;

            if (Configuration.Current.ActAs == null)
            {
                _logger.Debug("Start bind to {Domain} as a process user", domain);
                connection.Bind();
            }
            else
            {
                _logger.Debug("Start bind to {Domain} as a specified 'act-as' user", domain);
                connection.Bind(Configuration.Current.ActAs);
            }
            
            return connection;  
        }
        
        /// <summary>
        /// Creates new connection to a ldap domain, binds with the specified credential using Negotiate auth type and returns it.
        /// </summary>
        /// <param name="domain">LDAP domain.</param>
        /// <param name="identity">user LDAP identity</param>
        /// <param name="password">Password.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public LdapConnection Create(string domain, LdapIdentity identity, string password)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException($"'{nameof(domain)}' cannot be null or whitespace.", nameof(domain));
            }

            if (string.IsNullOrWhiteSpace(identity.Name))
            {
                throw new ArgumentException($"'{nameof(identity.Name)}' cannot be null or whitespace.", nameof(identity.Name));
            }

            if (password is null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            _logger.Debug("Start connection to {Domain}", domain);
            var connection = new LdapConnection(domain);
            connection.Credential = new NetworkCredential(identity.Name, password);
            if (identity.Type == IdentityType.UserPrincipalName)
            {
                connection.AuthType = AuthType.Basic;
            }
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.RootDseCache = true;

            _logger.Debug("Start bind to {Domain} as '{User}'", domain, identity.Name);
            connection.Bind();
            return connection;  
        }
    }
}