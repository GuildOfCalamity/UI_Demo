using Microsoft.Windows.Widgets.Providers;
using System;
using System.Threading;

namespace UI_Demo
{
    public class RegistrationManager<TWidgetProvider> : IDisposable where TWidgetProvider : IWidgetProvider, new()
    {
        IDisposable registeredProvider;
        bool disposedValue = false;
        ManualResetEvent disposedEvent = new ManualResetEvent(false);

        RegistrationManager(IDisposable provider)
        {
            registeredProvider = provider;
        }

        public static RegistrationManager<TWidgetProvider> RegisterProvider()
        {
            var registration = RegisterClass(typeof(TWidgetProvider).GUID, new WidgetProviderFactory<TWidgetProvider>());
            return new RegistrationManager<TWidgetProvider>(registration);
        }

        public ManualResetEvent GetDisposedEvent() => disposedEvent;

        static IDisposable RegisterClass(Guid clsid, Com.IClassFactory factory)
        {
            uint registrationHandle;
            Com.ClassObject.Register(clsid, factory, out registrationHandle);

            return new ClassLifetimeUnregister(registrationHandle);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                registeredProvider.Dispose();
                disposedValue = true;
                disposedEvent.Set();
            }
        }

        ~RegistrationManager() =>  Dispose(disposing: false);

        class ClassLifetimeUnregister : IDisposable
        {
            public ClassLifetimeUnregister(uint registrationHandle) { COMRegistrationHandle = registrationHandle; }
            private readonly uint COMRegistrationHandle;

            public void Dispose()
            {
                Com.ClassObject.Revoke(COMRegistrationHandle);
            }
        }
    }
}