// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.FormatConverters;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registers the ADF &lt;-&gt; HTML converter with a dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IAdfHtmlConverter"/>/<see cref="AdfHtmlConverter"/> as a singleton.
        /// </summary>
        /// <param name="services">The service collection to add the registration to.</param>
        /// <returns>The service collection, for chaining.</returns>
        public static IServiceCollection AddAdfHtmlConverter(this IServiceCollection services)
        {
            return services.AddSingleton<IAdfHtmlConverter, AdfHtmlConverter>();
        }
    }
}
