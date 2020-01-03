using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using SmartHome.Utils;

namespace SmartHome.Services
{
    public interface IServiceDiscovery
    {
        EventProxy<ServiceDiscovery.IPAddressEventArgs> DiscoverService(string serviceName);
        void StartService();
    }

    public class ServiceDiscovery : IDisposable, IServiceDiscovery
    {
        private readonly object _dictLock = new object();

        public ServiceDiscovery(ILogger<ServiceDiscovery> logger)
        {
            MDNS = new MulticastService();
            SD = new Makaretu.Dns.ServiceDiscovery(MDNS);
            Logger = logger;
            TargetDictionary = new Dictionary<string, HashSet<string>>();
            DomainNameDictionary = new Dictionary<string, IPAddress>();
            EventDictionary = new Dictionary<string, EventProxy<IPAddressEventArgs>>();
        }

        private MulticastService MDNS { get; }
        private Makaretu.Dns.ServiceDiscovery SD { get; }
        private ILogger<ServiceDiscovery> Logger { get; }
        private Dictionary<string, HashSet<string>> TargetDictionary { get; }
        private Dictionary<string, IPAddress> DomainNameDictionary { get; }
        private Dictionary<string, EventProxy<IPAddressEventArgs>> EventDictionary { get; }
        private bool ServiceStarted { get; set; }

        public void Dispose()
        {
            MDNS?.Dispose();
            SD?.Dispose();
        }

        public EventProxy<IPAddressEventArgs> DiscoverService(string serviceName)
        {
            MDNS.SendQuery(serviceName, type: DnsType.PTR);
            lock (_dictLock)
            {
                EventDictionary.TryAdd(serviceName, new EventProxy<IPAddressEventArgs>());
            }

            return EventDictionary[serviceName];
        }

        public void StartService()
        {
            if (ServiceStarted) return;
            ServiceStarted = true;
            MDNS.Start();
            SD.ServiceInstanceDiscovered += (s, e) => MDNS.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);

            MDNS.AnswerReceived += (s, e) =>
            {
                var servers = e.Message.Answers.OfType<SRVRecord>();
                foreach (var server in servers)
                {
                    var target = server.Target.ToString();
                    var name = string.Join(".", server.Name.Labels.TakeLast(3));
                    lock (_dictLock)
                    {
                        if (TargetDictionary.ContainsKey(target))
                        {
                            if (!TargetDictionary[target].Contains(name))
                                TargetDictionary[target].Add(name);
                        }
                        else
                        {
                            TargetDictionary.Add(target, new HashSet<string> {name});
                        }
                    }

                    Logger.LogInformation($"Receive service record: {name} = {target}");
                    MDNS.SendQuery(server.Target, type: DnsType.A);
                }

                var addresses = e.Message.Answers.OfType<AddressRecord>();
                foreach (var address in addresses)
                {
                    if (!DomainNameDictionary.TryAdd(address.Name.ToString(), address.Address))
                        DomainNameDictionary[address.Name.ToString()] = address.Address;
                    Logger.LogInformation($"Receive address record: {address.Name} = {address.Address}");
                    if (TargetDictionary.ContainsKey(address.Name.ToString()))
                        TargetDictionary[address.Name.ToString()]
                            .Where(t => EventDictionary.ContainsKey(t))
                            .Foreach(t => EventDictionary[t].Invoke(this, new IPAddressEventArgs(address.Address)));
                }
            };

            Logger.LogInformation("Service discovery started");
        }

        public class IPAddressEventArgs : EventArgs
        {
            public IPAddressEventArgs(IPAddress ipAddress)
            {
                IPAddress = ipAddress;
            }

            public IPAddress IPAddress { get; }
        }
    }
}