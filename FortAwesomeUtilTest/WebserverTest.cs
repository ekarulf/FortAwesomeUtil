using FortAwesomeUtil.Webserver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using FortAwesomeUtil.Webserver.Framework;

namespace FortAwesomeUtilTest
{
    class DummyWebservice: Webservice
    {
        [Path("/")]
        [Path("basic/")]
        public void BasicMethod(HttpListenerContext context)
        {
            // stub
        }

        [Path("argument/:id/")]
        public void ArgumentMethod(HttpListenerContext context, int id)
        {
            // stub
        }
    }
    
    /// <summary>
    ///This is a test class for WebserverTest and is intended
    ///to contain all WebserverTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WebserverTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ResolveWebservice
        ///</summary>
        [TestMethod()]
        [DeploymentItem("FortAwesomeUtil.dll")]
        public void ResolveWebserviceTest()
        {
            Webserver server = new Webserver();
            server.Bind("http://+:80/Temporary_Listen_Addresses/FortAwesomeUtil.test/base/");
            var dummy1 = new DummyWebservice();
            server.RegisterWebservice("foo1/", dummy1);
            var dummy2 = new DummyWebservice();
            server.RegisterWebservice("foo2/a/", dummy2);
            server.RegisterWebservice("foo2/b/", dummy2);
            server.Start();

            string url = "http://127.0.0.1:80/Temporary_Listen_Addresses/FortAwesomeUtil.test/base/foo2/a/basic/";
            Webservice expected = dummy1; // TODO: Initialize to an appropriate value
            Webservice actual;
            actual = server.ResolveWebservice(url);
            Assert.AreEqual(expected, actual);
        }
    }
}
