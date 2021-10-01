using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Neo4jClient;
using Neo4JTest.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4JTest.Pages
{
    public class InputModel
    {
        public int id { get; set; }
        public string naziv { get; set; }
        public string opis { get; set; }
        public int kondTezina { get; set; }
        public int tehTezina { get; set; }
        public int duzinaTrase { get; set; }
        public int visinskaRazlika { get; set; }

    }
    public class GuideEditModel : PageModel
    {
        private readonly ILogger<GuideEditModel> _logger;
        private readonly IDriver _driver;
        private BoltGraphClient _client;
        public GuideEditModel(ILogger<GuideEditModel> logger)
        {
            _logger = logger;
            _driver = GraphDatabase.Driver("bolt://localhost:7687/", AuthTokens.Basic("admin", "admin"));
            _client = new BoltGraphClient(_driver);
        }

        #region Properties

        [BindProperty(SupportsGet = true)]
        public Guide Input { get; set; }

        [BindProperty]
        public string Username { get; set; }
        #endregion

        public async Task<IActionResult> OnGet(int id, string username)
        {
            await _client.ConnectAsync();

            var query = await _client.Cypher
                .Match("(g:Guide)")
                .Where("g.id IS NOT NULL")
                .AndWhere((Guide g) => g.id == id)
                .Return(g => g.As<Guide>()).ResultsAsync;

            Input = query.FirstOrDefault();

            Username = username;

            return Page();
        }

        public async Task<IActionResult> OnPostEdit(string username)
        {
            await _client.ConnectAsync();

            if (ModelState.IsValid)
            {
                await _client.Cypher
                    .Match("(g:Guide)")
                    .Where("g.id IS NOT NULL")
                    .AndWhere((Guide g) => g.id == Input.id)
                    .Set("g = $guide")
                    .WithParam("guide", Input)
                    .ExecuteWithoutResultsAsync();
            }

            return RedirectToPage("./GuideCreate", new { username = username });
        }
    }
}
