/*
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

using System.IO;

namespace LBi.LostDoc.Templating
{
    public class ScopedFileProvider : IFileProvider
    {
        public ScopedFileProvider(IFileProvider fileProvider, string basePath)
        {
            this.FileProvider = fileProvider;
            this.BasePath = basePath;
        }

        public IFileProvider FileProvider { get; protected set; }

        public string BasePath { get; protected set; }

        public bool FileExists(string path)
        {
            return this.FileProvider.FileExists(Path.Combine(this.BasePath, path));
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            return this.FileProvider.OpenFile(Path.Combine(this.BasePath, path), mode);
        }
    }
}