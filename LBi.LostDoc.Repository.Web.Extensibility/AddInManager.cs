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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInManager : IEnumerable<AddInPackage>
    {
        private PackageManager _packageManager;

        public AddInManager(AddInRepository repository, string installDirectory, string packageDirectory)
        {
            this.Repository = repository;
            this.InstallDirectory = installDirectory;
            this.PackageDirectory = packageDirectory;
             
            this._packageManager = new PackageManager(repository.NuGetRepository, packageDirectory);
            
        }

        public string PackageDirectory { get; protected set; }
        public string InstallDirectory { get; protected set; }
        public AddInRepository Repository { get; protected set; }

        public void Install(AddInPackage package)
        {
        }

        public void Uninstall(AddInPackage package)
        {
            
        }

        public IEnumerator<AddInPackage> GetEnumerator()
        {
            return this._packageManager.LocalRepository.GetPackages()
                                                       .Select(AddInPackage.Create)
                                                       .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
