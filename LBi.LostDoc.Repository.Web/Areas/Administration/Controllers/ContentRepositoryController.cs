﻿/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Areas.Administration.Controllers
{

    // TODO this whole controller is BL soup, but it "works"
    [AdminController("repository", Text = "Repository", Group = Groups.Core, Order = 3000)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ContentRepositoryController : Controller
    {

        [AdminAction("index", IsDefault = true)]
        public ActionResult Index()
        {
            var ldocFiles = Directory.GetFiles(App.Instance.Content.RepositoryPath, "*.ldoc", SearchOption.TopDirectoryOnly);
            var ldocs = ldocFiles.Select(p => new LostDocFileInfo(p));
            var groups = ldocs.GroupBy(ld => ld.PrimaryAssembly.AssetId);

            List<AssemblyModel> assemblies = new List<AssemblyModel>();

            foreach (var group in groups)
            {
                assemblies.Add(new AssemblyModel
                                   {
                                       Name = group.First().PrimaryAssembly.Name,
                                       Versions = group.Select(ld =>
                                                               new VersionModel
                                                                   {
                                                                       Filename = Path.GetFileName(ld.Path),
                                                                       Created = System.IO.File.GetCreationTime(ld.Path),
                                                                       Version = ld.PrimaryAssembly.AssetId.Version
                                                                   }).ToArray()
                                   });
            }

            return this.View(new ContentRepositoryModel
                                 {
                                     Assemblies = assemblies.ToArray()
                                 });
        }

        // yeah, the {id} is still there...
        [HttpGet]
        public ActionResult Download(string id)
        {
            // TODO this security check might not be good enough
            if (Directory.GetFiles(App.Instance.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length == 1)
            {
                return File(Path.Combine(App.Instance.Content.RepositoryPath, id), "text/xml", id);
            }

            return new HttpStatusCodeResult(HttpStatusCode.NotFound, id + " not found");
        }

        [HttpPost]
        public ActionResult Delete(string id)
        {
            // TODO this security check might not be good enough
            if (Directory.GetFiles(App.Instance.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length == 1)
            {
                System.IO.File.Delete(Path.Combine(App.Instance.Content.RepositoryPath, id));

                App.Instance.Notifications.Add(Severity.Information,
                                           Lifetime.Page,
                                           Scope.User,
                                           this.User,
                                           "File removed",
                                           string.Format("Successfully deleted file: '{0}'.", id));

                return this.RedirectToAction("Index");
            }

            App.Instance.Notifications.Add(Severity.Error,
                                           Lifetime.Page,
                                           Scope.User,
                                           this.User,
                                           "File not found",
                                           string.Format("Unable to delete file '{0}' as it was not found.", id));

            return this.RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Upload()
        {
            return null;
        }

    }
}
