/*
 * [The "BSD licence"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2011 Sam Harwell, Tunnel Vision Laboratories, LLC
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.Test.StringTemplate
{
    using Antlr4.StringTemplate;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Antlr4.StringTemplate.Misc;

    [TestClass]
    public class TestModelAdaptors : BaseTest
    {
        private class UserAdaptor : ModelAdaptor
        {
            public object getProperty(ST self, object o, object property, string propertyName)
            {
                if (propertyName.Equals("id"))
                    return ((User)o).id;
                if (propertyName.Equals("name"))
                    return ((User)o).Name;
                throw new STNoSuchPropertyException(null, "User." + propertyName);
            }
        }

        private class UserAdaptorConst : ModelAdaptor
        {
            public object getProperty(ST self, object o, object property, string propertyName)
            {
                if (propertyName.Equals("id"))
                    return "const id value";
                if (propertyName.Equals("name"))
                    return "const name value";
                throw new STNoSuchPropertyException(null, "User." + propertyName);
            }
        }

        private class SuperUser : User
        {
            int bitmask;

            public SuperUser(int id, string name)
                : base(id, name)
            {
                bitmask = 0x8080;
            }

            public override string Name
            {
                get
                {
                    return "super " + base.Name;
                }
            }
        }

        [TestMethod]
        public void TestSimpleAdaptor()
        {
            string templates =
                    "foo(x) ::= \"<x.id>: <x.name>\"\n";
            writeFile(tmpdir, "foo.stg", templates);
            STGroup group = new STGroupFile(tmpdir + "/foo.stg");
            group.registerModelAdaptor(typeof(User), new UserAdaptor());
            ST st = group.getInstanceOf("foo");
            st.add("x", new User(100, "parrt"));
            string expecting = "100: parrt";
            string result = st.render();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestAdaptorAndBadProp()
        {
            ErrorBufferAllErrors errors = new ErrorBufferAllErrors();
            string templates =
                    "foo(x) ::= \"<x.qqq>\"\n";
            writeFile(tmpdir, "foo.stg", templates);
            STGroup group = new STGroupFile(tmpdir + "/foo.stg");
            group.setListener(errors);
            group.registerModelAdaptor(typeof(User), new UserAdaptor());
            ST st = group.getInstanceOf("foo");
            st.add("x", new User(100, "parrt"));
            string expecting = "";
            string result = st.render();
            Assert.AreEqual(expecting, result);

            STRuntimeMessage msg = (STRuntimeMessage)errors.Errors[0];
            STNoSuchPropertyException e = (STNoSuchPropertyException)msg.Cause;
            Assert.AreEqual("User.qqq", e.PropertyName);
        }

        [TestMethod]
        public void TestAdaptorCoversSubclass()
        {
            string templates =
                    "foo(x) ::= \"<x.id>: <x.name>\"\n";
            writeFile(tmpdir, "foo.stg", templates);
            STGroup group = new STGroupFile(tmpdir + "/foo.stg");
            group.registerModelAdaptor(typeof(User), new UserAdaptor());
            ST st = group.getInstanceOf("foo");
            st.add("x", new SuperUser(100, "parrt")); // create subclass of User
            string expecting = "100: super parrt";
            string result = st.render();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestWeCanResetAdaptorCacheInvalidatedUponAdaptorReset()
        {
            string templates =
                    "foo(x) ::= \"<x.id>: <x.name>\"\n";
            writeFile(tmpdir, "foo.stg", templates);
            STGroup group = new STGroupFile(tmpdir + "/foo.stg");
            group.registerModelAdaptor(typeof(User), new UserAdaptor());
            group.getModelAdaptor(typeof(User)); // get User, SuperUser into cache
            group.getModelAdaptor(typeof(SuperUser));

            group.registerModelAdaptor(typeof(User), new UserAdaptorConst());
            // cache should be reset so we see new adaptor
            ST st = group.getInstanceOf("foo");
            st.add("x", new User(100, "parrt"));
            string expecting = "const id value: const name value"; // sees UserAdaptorConst
            string result = st.render();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSeesMostSpecificAdaptor()
        {
            string templates =
                    "foo(x) ::= \"<x.id>: <x.name>\"\n";
            writeFile(tmpdir, "foo.stg", templates);
            STGroup group = new STGroupFile(tmpdir + "/foo.stg");
            group.registerModelAdaptor(typeof(User), new UserAdaptor());
            group.registerModelAdaptor(typeof(SuperUser), new UserAdaptorConst()); // most specific
            ST st = group.getInstanceOf("foo");
            st.add("x", new User(100, "parrt"));
            string expecting = "100: parrt";
            string result = st.render();
            Assert.AreEqual(expecting, result);

            st.remove("x");
            st.add("x", new SuperUser(100, "parrt"));
            expecting = "const id value: const name value"; // sees UserAdaptorConst
            result = st.render();
            Assert.AreEqual(expecting, result);
        }
    }
}
