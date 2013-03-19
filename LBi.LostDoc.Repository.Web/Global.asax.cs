﻿/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.WebHost.Routing;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using LBi.LostDoc;
using LBi.LostDoc.Packaging;
using LBi.LostDoc.Packaging.Composition;
using LBi.LostDoc.Repository.Web.Areas.Administration;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Notifications;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;
using ContractNames = LBi.LostDoc.Composition.ContractNames;

namespace LBi.LostDoc.Repository.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            
            // this filter injects Notifications into the IBaseModel
            filters.Add(new AdminFilter());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapHttpRoute(
                                name: "Repository",
                                routeTemplate: "site/repository/{assembly}/{version}",
                                defaults: new
                                              {
                                                  controller = "Repository",
                                                  assembly = RouteParameter.Optional,
                                                  version = RouteParameter.Optional
                                              });


            routes.MapHttpRoute(
                                name: "Admin",
                                routeTemplate: "site/library",
                                defaults: new
                                              {
                                                  controller = "Library",
                                              });

            routes.MapHttpRoute(
                                name: "Rebuild",
                                routeTemplate: "site/rebuild",
                                defaults: new
                                              {
                                                  controller = "Site",
                                                  action = "Rebuild",
                                              });

            routes.MapHttpRoute(
                                name: "Status",
                                routeTemplate: "site/status",
                                defaults: new
                                              {
                                                  controller = "Site",
                                                  action = "GetStatus"
                                              });


            routes.MapHttpRoute(
                                name: "Search",
                                routeTemplate: "archive/{id}/search/{searchTerms}",
                                defaults: new
                                              {
                                                  controller = "Search",
                                                  action = "Get"
                                              });


            routes.MapHttpRoute(
                                name: "DefaultSearch",
                                routeTemplate: "library/search/{searchTerms}",
                                defaults: new
                                              {
                                                  controller = "Search",
                                                  id = "current",
                                                  action = "Get"
                                              });


            routes.MapRoute(
                            name: "Archive",
                            url: "archive/{id}/{*path}",
                            defaults: new
                                          {
                                              controller = "Content",
                                              id = "current",
                                              action = "GetContent",
                                              path = "Library.html"
                                          });

            routes.MapRoute(
                            name: "Library",
                            url: "library/{*path}",
                            defaults: new
                                          {
                                              controller = "Content",
                                              id = "current",
                                              path = "Library.html",
                                              action = "GetContent"
                                          });


            routes.Add("Redirect", new Route("", new RedirectRouteHandler("Library")));
        }

        protected void Application_Start()
        {
            // TODO maybe put this somewhere else (not in global.asax)
            // TODO maybe move all of this into the App class with "IAppConfig"

            // initialize logger
            TraceListener traceListener = new TextWriterTraceListener(Path.Combine(AppConfig.LogPath, string.Format("repository_{0:yyyy'-'MM'-'dd__HHmmss}.log", DateTime.Now)));
            Web.TraceSources.RepositoryControllerSource.Listeners.Add(traceListener);
            Repository.TraceSources.ContentManagerSource.Listeners.Add(traceListener);
            Repository.TraceSources.ContentSearcherSource.Listeners.Add(traceListener);

            // this might be stupid, but it fixes things for iisexpress
            Directory.SetCurrentDirectory(HostingEnvironment.ApplicationPhysicalPath);

            // set up add-in system
            AddInSource officalSource = new AddInSource("Official LostDoc repository add-in feed",
                                                        AppConfig.AddInRepository,
                                                        isOfficial: true);

            // intialize MEF

            // core 'add-ins'
            var currentAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = currentAssembly.GetName();
            string corePackageId = assemblyName.Name;
            string corePackageVersion = assemblyName.Version.ToString();
            AggregateCatalog catalog =
                new AggregateCatalog(new AddInCatalog(new ApplicationCatalog(), corePackageId, corePackageVersion));

            // load other sources from site-settings (not config)
            AddInRepository repository = new AddInRepository(officalSource);
            AddInManager addInManager = new AddInManager(repository, AppConfig.AddInInstallPath, AppConfig.AddInPackagePath);
            
            // hook event so that installed add-ins get registered in the catalog, if composition occurs after this fires
            // or if recomposition is enabled, no restart should be requried
            addInManager.Installed +=
                (sender, args) => catalog.Catalogs.Add(new AddInCatalog(new DirectoryCatalog(args.InstallationPath),
                                                                        args.Package.Id,
                                                                        args.Package.Version));
            
            // delete and redeploy all installed packages, this will trigger the Installed event ^
            // this acts as a crude "remove/overwrite plugins that were in use when un/installed" hack
            addInManager.Restore();

   
            // create container
            CompositionContainer container = new CompositionContainer(catalog);

            // set up template resolver
            var lazyProviders = container.GetExports<IFileProvider>(ContractNames.TemplateProvider);
            var realProviders = lazyProviders.Select(lazy => lazy.Value);
            TemplateResolver templateResolver = new TemplateResolver(realProviders.ToArray());

            // load template
            Template template = new Template(container);
            template.Load(templateResolver, AppConfig.Template);

            // set up content manager
            ContentManager contentManager = new ContentManager(new ContentSettings
                                          {
                                              ContentPath = AppConfig.ContentPath,
                                              // TODO make this configurable
                                              IgnoreVersionComponent = VersionComponent.Patch,
                                              RepositoryPath = AppConfig.RepositoryPath,
                                              Template = template
                                          });


            // set up notifaction system
            NotificationManager notifications = new NotificationManager();

            // initialize app-singleton
            App.Initialize(container, contentManager, addInManager, notifications, traceListener);


            // MVC init
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);


            IControllerFactory oldControllerFactory = ControllerBuilder.Current.GetControllerFactory();
            IControllerFactory newControllerFactory = new AddInControllerFactory(AdministrationAreaRegistration.Name, container, oldControllerFactory);
            ControllerBuilder.Current.SetControllerFactory(newControllerFactory);
        }

    }
}
