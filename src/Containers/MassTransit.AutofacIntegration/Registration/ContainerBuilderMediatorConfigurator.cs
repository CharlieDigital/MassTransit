namespace MassTransit.AutofacIntegration.Registration
{
    using System;
    using Autofac;
    using MassTransit.Registration;
    using Mediator;
    using ScopeProviders;
    using Scoping;


    public class ContainerBuilderMediatorConfigurator :
        RegistrationConfigurator,
        IContainerBuilderMediatorConfigurator
    {
        readonly ContainerBuilder _builder;
        readonly AutofacContainerRegistrar _registrar;
        Action<IRegistration, IReceiveEndpointConfigurator> _configure;

        public ContainerBuilderMediatorConfigurator(ContainerBuilder builder)
            : this(builder, new AutofacContainerMediatorRegistrar(builder))
        {
        }

        ContainerBuilderMediatorConfigurator(ContainerBuilder builder, AutofacContainerRegistrar registrar)
            : base(registrar)
        {
            _builder = builder;
            _registrar = registrar;

            ScopeName = "message";

            builder.Register(CreateConsumerScopeProvider)
                .As<IConsumerScopeProvider>()
                .SingleInstance()
                .IfNotRegistered(typeof(IConsumerScopeProvider));

            builder.Register(context => new AutofacConfigurationServiceProvider(context.Resolve<ILifetimeScope>()))
                .As<IConfigurationServiceProvider>()
                .SingleInstance()
                .IfNotRegistered(typeof(IConfigurationServiceProvider));

            builder.Register(MediatorFactory)
                .As<IMediator>()
                .SingleInstance();
        }

        public string ScopeName
        {
            private get => _registrar.ScopeName;
            set => _registrar.ScopeName = value;
        }

        ContainerBuilder IContainerBuilderMediatorConfigurator.Builder => _builder;

        public Action<ContainerBuilder, ConsumeContext> ConfigureScope
        {
            get => _registrar.ConfigureScope;
            set => _registrar.ConfigureScope = value;
        }

        public void ConfigureMediator(Action<IRegistration, IReceiveEndpointConfigurator> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            ThrowIfAlreadyConfigured(nameof(ConfigureMediator));
            _configure = configure;
        }

        IMediator MediatorFactory(IComponentContext context)
        {
            var provider = context.Resolve<IConfigurationServiceProvider>();

            ConfigureLogContext(provider);

            return Bus.Factory.CreateMediator(cfg =>
            {
                ConfigureMediator(cfg, provider, _configure);
            });
        }

        IConsumerScopeProvider CreateConsumerScopeProvider(IComponentContext context)
        {
            var lifetimeScopeProvider = new SingleLifetimeScopeProvider(context.Resolve<ILifetimeScope>());
            return new AutofacConsumerScopeProvider(lifetimeScopeProvider, ScopeName, ConfigureScope);
        }
    }
}