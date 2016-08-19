#region References

using System;
using System.Web;
using System.Web.Http;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Newtonsoft.Json;
using Ninject;
using Ninject.Web.Common;
using Ninject.WebApi.DependencyResolver;
using Scribe.Data;
using Scribe.Services;
using Scribe.Website;
using Scribe.Website.Hubs;
using Scribe.Website.Services;
using Scribe.Website.Services.Notifications;
using WebActivatorEx;

#endregion

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(NinjectWebCommon), "Start")]
[assembly: ApplicationShutdownMethod(typeof(NinjectWebCommon), "Stop")]

namespace Scribe.Website
{
	public static class NinjectWebCommon
	{
		#region Fields

		private static readonly Bootstrapper _bootstrapper = new Bootstrapper();

		#endregion

		#region Methods

		/// <summary>
		/// Starts the application
		/// </summary>
		public static void Start()
		{
			DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
			DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
			_bootstrapper.Initialize(CreateKernel);
		}

		/// <summary>
		/// Stops the application.
		/// </summary>
		public static void Stop()
		{
			_bootstrapper.ShutDown();
		}

		/// <summary>
		/// Creates the kernel that will manage your application.
		/// </summary>
		/// <returns> The created kernel. </returns>
		private static IKernel CreateKernel()
		{
			var kernel = new StandardKernel();
			try
			{
				kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
				kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();
				GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(kernel);
				RegisterServices(kernel);
				return kernel;
			}
			catch
			{
				kernel.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Load your modules or register your services here!
		/// </summary>
		/// <param name="kernel"> The kernel. </param>
		private static void RegisterServices(IKernel kernel)
		{
			var settings = new JsonSerializerSettings();
			settings.ContractResolver = new SignalRContractResolver();
			var serializer = JsonSerializer.Create(settings);

			kernel.Bind<JsonSerializer>().ToConstant(serializer);
			kernel.Bind<IScribeDatabase>().To<ScribeSqlDatabase>();
			kernel.Bind<IAuthenticationService>().To<AuthenticationService>();
			kernel.Bind<INotificationHub>().To<NotificationHubService>();
			kernel.Bind<INotificationService>().To<SmtpNotificationService>();
		}

		#endregion
	}
}