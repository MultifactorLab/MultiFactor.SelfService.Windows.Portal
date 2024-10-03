using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class ForestSchemaLoader
    {
        private readonly LdapConnectionAdapter _connectionAdapter;
        private readonly Configuration _configuration;
        private readonly ILogger _logger;

        private const string CommonNameAttribute = "cn";
        private const string UpnSuffixesAttribute = "uPNSuffixes";

        public ForestSchemaLoader(Configuration configuration, LdapConnection connection, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionAdapter = new LdapConnectionAdapter(connection, _logger);
        }

        public ForestSchema Load(LdapIdentity root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            _logger.Debug("Loading forest schema from {Root:l}", root);

            var domainNameSuffixes = new Dictionary<string, LdapIdentity>();
            try
            {
                var trustedDomainsResult = _connectionAdapter.Query(
                    "CN=System," + root.Name,
                    "objectClass=trustedDomain",
                    SearchScope.OneLevel,
                    true,
                    CommonNameAttribute);

                var schema = new List<LdapIdentity> { root };
                var trustedDomains = trustedDomainsResult.GetAttributeValuesByName(CommonNameAttribute)
                    .Where(domain => _configuration.IsPermittedDomain(domain))
                    .Select(LdapIdentity.FqdnToDn);

                foreach (var domain in trustedDomains)
                {
                    _logger.Debug("Found trusted domain: {Domain:l}", domain);
                    schema.Add(domain);
                }

                foreach (var domain in schema)
                {
                    var domainSuffix = domain.DnToFqdn();
                    if (!domainNameSuffixes.ContainsKey(domainSuffix))
                    {
                        domainNameSuffixes.Add(domainSuffix, domain);
                    }

                    var isChild = schema.Any(parent => domain.IsChildOf(parent));
                    if (isChild)
                    {
                        continue;
                    }

                    try
                    {
                        var uPNSuffixesResult = _connectionAdapter.Query(
                            $"CN=Partitions,CN=Configuration,{domain.Name}",
                            "objectClass=*",
                            SearchScope.Base,
                            true,
                            UpnSuffixesAttribute);
                        List<string> uPNSuffixes = uPNSuffixesResult.GetAttributeValuesByName(UpnSuffixesAttribute);

                        foreach (var suffix in uPNSuffixes.Where(upn => !domainNameSuffixes.ContainsKey(upn)))
                        {
                            domainNameSuffixes.Add(suffix, domain);
                            _logger.Debug("Found alternative UPN suffix {Suffix:l} for domain {Domain}", suffix,
                                domain);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Unable to query {Domain:l}", domain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to load forest schema");
            }

            return new ForestSchema(domainNameSuffixes);
        }
    }
}