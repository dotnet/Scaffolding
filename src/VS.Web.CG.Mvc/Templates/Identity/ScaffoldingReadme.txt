The code for Identity UI has been generated under Areas/Identity.
Content files have been added under wwwroot/Identity

However you may need to make additional changes to the Startup's ConfigureServices method:

1. Enable Areas support for RazorPages:

    services.AddMvc()
    .AddRazorPagesOptions(options => options.AllowAreas = true);


2. Set up the Identity cookie paths

    services.ConfigureApplicationCookie(options => 
    {
        options.LoginPath = "/Identity/Account/Login";
        options.LogoutPath = "/Identity/Account/Logout";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    });

3. Add EmailSender to services

    services.Add<IEmailSender, EmailSender>();
