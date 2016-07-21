﻿using System;
using System.Collections.Generic;
using System.Linq;
using LightInject;
using Umbraco.Core.Logging;
using Umbraco.Core.ObjectResolution;

namespace Umbraco.Core.Persistence.Migrations
{
    /// <summary>
    /// A resolver to return all IMigrations
    /// </summary>
    internal class MigrationResolver : ContainerLazyManyObjectsResolver<MigrationResolver, IMigration>, IMigrationResolver
    {

        public MigrationResolver(IServiceContainer container, ILogger logger, Func<IEnumerable<Type>> migrations)
            : base(container, logger, migrations, ObjectLifetimeScope.Transient) // do NOT change .Transient, see CreateValues below
        {
        }

        /// <summary>
        /// This creates the instances in a child IoC container, everytime GetMigrations is called, a child container
        /// is created, the types are added to it and resolved and the child container is diposed.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        /// <remarks>
        /// This doesn't need to be thread safe because Migration instances are transient anyways
        /// </remarks>
        protected override IEnumerable<IMigration> CreateValues(ObjectLifetimeScope scope)
        {
            // note: constructor dependencies do NOT work with lifetimes other than transient
            // see https://github.com/seesharper/LightInject/issues/294
            EnsureTypesRegisterred(scope, container =>
            {
                // resolve ctor dependency from GetInstance() runtimeArguments, if possible - 'factory' is
                // the container, 'info' describes the ctor argument, and 'args' contains the args that
                // were passed to GetInstance() - use first arg if it is the right type,
                //
                // for IMigrationContext
                container.RegisterConstructorDependency((factory, info, args) => args.Length > 0 ? args[0] as IMigrationContext : null);
            });

            var arg = new object[] { _migrationContext };
            return InstanceTypes.Select(x => (IMigration) Container.GetInstance(x, arg));
        }

        private IMigrationContext _migrationContext;

        /// <summary>
        /// Gets the migrations
        /// </summary>
        public IEnumerable<IMigration> GetMigrations(IMigrationContext migrationContext)
        {
            //set the current context to use to create the values
            _migrationContext = migrationContext;

            return Values;
        }
    }
}