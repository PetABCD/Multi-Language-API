using Multi_language.ApiHelper.Client;
using Multi_language.Common.Infrastructure.Manifest;
using Multi_Language.MVCClient.ApiInfrastructure;
using Multi_Language.MVCClient.ApiInfrastructure.Client;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Multi_Language.MVCClient.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(Multi_Language.MVCClient.App_Start.NinjectWebCommon), "Stop")]

namespace Multi_Language.MVCClient.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;
    using Multi_language.ApiHelper;
    using Ninject.Extensions.Conventions;
    using Ninject;
    using Ninject.Web.Common;
    using System.Web.Mvc;
    using Ninject.Web.Mvc;
    using Multi_language.Data;

    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }

        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);


                DependencyResolver.SetResolver(new NinjectDependencyResolver(kernel));
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
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            kernel
       .Bind<IDbContext>()
       .To<MultiLanguageDbContext>()
       .InRequestScope();

            kernel.Bind(typeof(IRepository<>)).To(typeof(EfGenericRepository<>));


            kernel.Bind(b => b.From("Multi-language.Services")
            .SelectAllClasses()
            .BindDefaultInterface());

            // TODO Find a way to bind custom interfaces with their default values
            // kernel.Bind(b => b.From("Multi-Language.MVCClient")
            //.SelectAllClasses()
            //.BindDefaultInterface());HttpClientInstance.Instance, tokenContainer

            kernel.Bind<ITokenContainer>().To<TokenContainer>().InSingletonScope();

            kernel.Bind<IApiClient>().To<ApiClient>()
                .InSingletonScope()
                .WithConstructorArgument("httpClient", HttpClientInstance.Instance)
                .WithConstructorArgument("tokenContainer", kernel.Get<ITokenContainer>());

            kernel.Bind<IBackupClient>().To<BackupClient>()
                .InSingletonScope().WithConstructorArgument("apiClient", kernel.Get<IApiClient>());

            kernel.Bind<ISystemInfoClient>().To<SystemInfoClient>()
               .InSingletonScope().WithConstructorArgument("apiClient", kernel.Get<IApiClient>());


            kernel.Bind<IManifestService>().To<ManifestService>();

            //kernel.Bind(b => b.From("Multi_language.ApiHelper")
            //     .SelectAllClasses()
            //     .BindDefaultInterface());

        }
    }
}
