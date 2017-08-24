# MoqBotLite

This project was created to address a few problems I had with the Ninject.Moq automocker. I wanted a fresh Kernel for each test (without calling Reset) and I wanted to minimize the setup necessary for verifiable mock objects.

MoqBotLite does not depend on an IoC container - it has one built in.

Using MoqBot, this:

    [Test]
    public void TestComponent_CallingRunAll_CallsServiceARunA()
    {
        Kernel.Reset();
        var mockServiceA = new Mock<IServiceA>(MockBehavior.Strict);
        mockServiceA.Setup(x => x.RunA()).Verifiable();
        Kernel.Bind<IServiceA>().ToConstant(mockServiceA.Object);
        Kernel.Bind<TestComponent>().ToSelf();

        Kernel.Get<TestComponent>().RunAll();

        mockServiceA.Verify();
    }

becomes this:

    [Test]
    public void TestComponent_CallingRunAll_CallsServiceARunA()
    {
        using (var bot = new MoqBot())
        {
            bot.Mock<IServiceA>().Setup(x => x.RunA()).Verifiable();
            bot.Get<TestComponent>().RunAll();
        }
    }

MoqBot borrows ideas from MoqContrib's AutoMoq and Ninject's Ninject.Moq.
