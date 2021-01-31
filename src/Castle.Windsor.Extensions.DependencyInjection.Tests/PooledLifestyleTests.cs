// Copyright 2004-2021 Castle Project - http://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Windsor.Extensions.DependencyInjection.Tests
{
	using Castle.Facilities.TypedFactory;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Lifestyle;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor.Extensions.DependencyInjection.Tests.Components;
	using Castle.Windsor.Installer;
	using Castle.Windsor.Proxy;

	using Microsoft.Extensions.DependencyInjection;

	using Xunit;

	public class PooledLifestyleTests
	{
		[Fact]
		public void PooledLifetimeNotHandledCorrectly()
		{
			// Arrange
			var serviceCollection = new ServiceCollection();
			var windsorContainer =
				new WindsorContainer(
					new DefaultKernel(
						new DefaultProxyFactory()),
					new DefaultComponentInstaller());

			var factory = new WindsorServiceProviderFactory(windsorContainer);

			windsorContainer
				.AddFacility<TypedFactoryFacility>();
			windsorContainer
				.Register(
					Component
						.For<IServiceB>()
						.ImplementedBy<ServiceB>()
						.LifestyleTransient(),
					Component
						.For<IServiceA>()
						.ImplementedBy<ServiceA>()
						.Named(nameof(ServiceA))
						.LifestylePooled(1, 2),
					Component
						.For<IFactoryForServiceA>()
						.AsFactory()
						.LifestyleTransient());

			var container = factory.CreateBuilder(serviceCollection);
			var provider = factory.CreateServiceProvider(container);

			// Act
			IServiceA pooledServiceA = null;
			using (var scope = windsorContainer.BeginScope())
			{
				var factoryForServiceA = windsorContainer.Resolve<IFactoryForServiceA>();
				try
				{
					pooledServiceA = factoryForServiceA.GetServiceA();
					Assert.NotNull(pooledServiceA);
					Assert.False(pooledServiceA.IsDisposed());
					Assert.NotNull(pooledServiceA.ServiceB);
					Assert.False(pooledServiceA.ServiceB.IsDisposed());
					factoryForServiceA.Release(pooledServiceA);
				}
				finally
				{
					windsorContainer.Release(factoryForServiceA);
				}
			}

			// Assert
			Assert.NotNull(pooledServiceA);
			Assert.False(pooledServiceA.IsDisposed());
			Assert.NotNull(pooledServiceA.ServiceB);
			Assert.False(pooledServiceA.ServiceB.IsDisposed());
		}
	}
}