using LogisticsAndDeliveries.Application.Packages.DriverSelection;
using LogisticsAndDeliveries.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsAndDeliveries.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Registrar servicios de la capa de aplicación
            services.AddDomain();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddScoped<IDriverSelectionService, DriverSelectionService>();
            return services;
        }
    }
}