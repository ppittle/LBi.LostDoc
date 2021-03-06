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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Company.Project.AnotherLibrary;
using Company.Project.Library;
using Xunit;

namespace LBi.LostDoc.Test
{
    public class NamingTests
    {
        [Fact]
        public void MethodOnGenericClass()
        {
            Assert.Equal("M:Company.Project.AnotherLibrary.SpecialClass.ReturnsGeneric",
                         Naming.GetAssetId(typeof(SpecialClass).GetMethod("ReturnsGeneric")));
        }

        [Fact]
        public void GenericMethodOnOpenGenericType_WithGenericParameter()
        {
            Assert.Equal(
                         "M:Company.Project.Library.GenericClass`1.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{`0},System.Collections.Generic.IEnumerable{System.Int32})",
                         Naming.GetAssetId(typeof(GenericClass<>).GetMethod("AnotherGeneric")));
        }

        [Fact]
        public void InheritedGenericMethodOnGenericClass_WithGenericParameter()
        {
            Assert.Equal(
                         "M:Company.Project.AnotherLibrary.SpecialClass.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{Company.Project.Library.RegularClass},System.Collections.Generic.IEnumerable{System.Int32})",
                         Naming.GetAssetId(typeof(SpecialClass).GetMethod("AnotherGeneric")));
        }


        [Fact(Skip = "Generic params no longer supported in Naming")]
        public void OverriddenGenericMethod__WithGenericParameter()
        {
            Assert.Equal(
                         "M:Company.Project.AnotherLibrary.InheritsSpecialClass.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{Company.Project.Library.RegularClass},System.Collections.Generic.IEnumerable{System.Int32})",
                         Naming.GetAssetId(typeof(InheritsSpecialClass).GetMethod("AnotherGeneric")));
        }

        [Fact]
        public void ClosedGenericType()
        {
            Assert.Equal("T:System.Collections.Generic.List`1",
                         Naming.GetAssetId(typeof(List<string>)));
        }

        [Fact]
        public void OpenGenericType()
        {
            Assert.Equal("T:System.Collections.Generic.List`1",
                         Naming.GetAssetId(typeof(List<>)));
        }

        [Fact]
        public void ConversionOperator()
        {
            Assert.Equal(
                         "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
                         Naming.GetAssetId(
                                           typeof(RegularClass).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                               .First(p => p.Name == "op_Explicit")));
        }

        [Fact]
        public void NestedOpenGenericType()
        {
            string aid = Naming.GetAssetId(typeof(GenericClass<>.NestedGeneric<>));
            Assert.Equal("T:Company.Project.Library.GenericClass`1.NestedGeneric`1", aid);
        }


        [Fact]
        public void NestedClosedGenericType()
        {
            string aid = Naming.GetAssetId(typeof(GenericClass<string>.NestedGeneric<int>));
            Assert.Equal("T:Company.Project.Library.GenericClass`1.NestedGeneric`1", aid);
        }


        [Fact]
        public void MethodOnNestedClosedGenericType()
        {
            string aid =
                Naming.GetAssetId(
                                  typeof(GenericClass<string>.NestedGeneric<int>).GetMethods().First(
                                                                                                     m =>
                                                                                                     m.Name.StartsWith
                                                                                                         ("ConsumeP")));
            Assert.Equal("M:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeP(System.Int32)", aid);
        }


        [Fact]
        public void OverriddenGenericMethod()
        {
            MethodInfo method = typeof(SpecialClass).GetMethods().First(m => m.Name == "ConsumesGeneric");

            Assert.Equal(
                         "M:Company.Project.AnotherLibrary.SpecialClass.ConsumesGeneric(Company.Project.Library.RegularClass)",
                         Naming.GetAssetId(method));
        }
    }
}
