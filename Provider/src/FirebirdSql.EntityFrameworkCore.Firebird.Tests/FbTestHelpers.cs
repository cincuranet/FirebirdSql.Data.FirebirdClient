using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore
{
    public class FbTestHelpers : TestHelpers
    {
        protected FbTestHelpers()
        {
        }

        public static FbTestHelpers Instance { get; } = new FbTestHelpers();

        public override IServiceCollection AddProviderServices(IServiceCollection services)
            => services.AddEntityFrameworkFb();

        protected override void UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseFirebird("Database=DummyDatabase");
    }
}