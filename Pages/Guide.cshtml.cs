using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Neo4jClient;
using Neo4JTest.Model;

namespace Neo4JTest.Pages
{
    public class GuideModel : PageModel
    {
        private readonly ILogger<GuideModel> _logger;
        private readonly IDriver _driver;
        private BoltGraphClient _client;
        public GuideModel(ILogger<GuideModel> logger)
        {
            _logger = logger;
            _driver = GraphDatabase.Driver("bolt://localhost:7687/", AuthTokens.Basic("admin", "admin"));
            _client = new BoltGraphClient(_driver);
        }

        [BindProperty(SupportsGet = true)]
        public Guide Guide { get; set; }

        public async Task<IActionResult> OnGet(int id)
        {
            await _client.ConnectAsync();

            var res = await _client.Cypher
                .Match("(g:Guide)-[:CONTAINS]->(i:Image)")
                .Where("g.id IS NOT NULL")
                .AndWhere((Guide g) => g.id == id)
                .Return((g, i) => new
                {
                    guide = g.As<Guide>(),
                    images = i.CollectAs<Image>()
                }).ResultsAsync;

            Guide = res.FirstOrDefault().guide;
            Guide.images = res.FirstOrDefault().images.ToList();


            return Page();
        }
    }
}
