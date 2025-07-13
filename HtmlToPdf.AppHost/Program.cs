var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HtmlToPdf>("htmltopdf");

builder.Build().Run();
