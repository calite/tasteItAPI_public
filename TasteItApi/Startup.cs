using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Neo4jClient;


namespace TasteItApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TasteIt_public", Version = "v1" });
            });

            services.AddCors(); //cors
            
            //NEO
            var client = new BoltGraphClient(new Uri("neo4j+s://dc95b24b.databases.neo4j.io"), "neo4j", "sBQ6Fj2oXaFltjizpmTDhyEO9GDiqGM1rG-zelf17kg");
            client.ConnectAsync();
            services.AddSingleton<IGraphClient>(client);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); //cors


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TasteIt_public v1"));
            }
            //swagger produccion
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TasteIt_public v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
