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

using System.ComponentModel.Composition.Hosting;
using System.Runtime.Caching;

namespace LBi.LostDoc
{
    public class FilterContext : IFilterContext
    {
        public FilterContext(ObjectCache cache, CompositionContainer container, IAssetResolver assetResolver, FilterState state)
        {
            this.Container = container;
            this.Cache = cache;
            this.AssetResolver = assetResolver;
            this.State = state;
        }

        #region IFilterContext Members

        public IAssetResolver AssetResolver { get; private set; }
        public FilterState State { get; private set; }

        #endregion

        public ObjectCache Cache { get; private set; }

        public CompositionContainer Container { get; private set; }
    }
}
