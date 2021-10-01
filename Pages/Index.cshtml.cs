using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Neo4JTest.Repositories;
using Neo4JTest.Model;
using Neo4jClient;
using Neo4j.Driver;
using Neo4jClient.Cypher;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Neo4JTest.Pages
{
    public class LoginInfo
    {
        [Required]
        public string username { get; set; }
        [Required]
        public string password { get; set; }
    }

    public class RegisterInfo
    {
        public string name { get; set; }
        public string surname { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IDriver _driver;
        private BoltGraphClient _client;
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            _driver = GraphDatabase.Driver("bolt://localhost:7687/", AuthTokens.Basic("admin", "admin"));
            _client = new BoltGraphClient(_driver);
        }

        [BindProperty]
        public LoginInfo LoginInfo { get; set; }
        [BindProperty(SupportsGet = true)]
        public List<Guide> Guides { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterString { get; set; }

        [BindProperty]
        public RegisterInfo NewUser { get; set; }

        public string Message { get; set; }

        public async Task<PageResult> OnGet(string message = null)
        {
            
            await _client.ConnectAsync();

            var searchString = ".*" + FilterString + ".*";
            var res = await _client.Cypher
                .Match("(g:Guide)-[:CONTAINS]->(i:Image)")
                .Where("g.naziv IS NOT NULL")
                .AndWhere("g.naziv =~ $searchString")
                .WithParam("searchString", searchString)
                .Return((g, i) => new
                {
                    guide = g.As<Guide>(),
                    images = i.CollectAs<Image>()
                }).ResultsAsync;

            foreach (var item in res)
            {
                var guide = item.guide;
                guide.images = item.images.ToList();
                Guides.Add(guide);
            }

            Message = message;
            return Page();
        }



        public async Task<IActionResult> OnPostLoginAsync()
        {
            await _client.ConnectAsync();

            if (ModelState.IsValid)
            {
                if (await CheckLogin())
                {
                    return RedirectToPage("./GuideCreate", new { username = LoginInfo.username});
                }
            }

            return RedirectToPage("./Index", new { message = "Pogresno ste uneli username ili password. Pokusajte ponovo." });
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            await _client.ConnectAsync();

            if (NewUser.name != null && NewUser.surname != null &&
                NewUser.username != null && NewUser.password != null)
            {
                await _client.Cypher
                    .Merge("(u:User {username: $uname})")
                    .OnCreate()
                    .Set("u = $NewUser")
                    .WithParams(new
                    {
                        uname = NewUser.username,
                        NewUser
                    })
                    .ExecuteWithoutResultsAsync();
                return RedirectToPage("./Index", new { message = "Uspesno ste se registrovali"});
            }
            return RedirectToPage("./Index", new { message = "Pokusajte ponovo" });
        }

        public async Task<bool> CheckLogin()
        {
            User user = (await _client.Cypher
                .Match("(u:User)")
                .Where((User u) => u.username == LoginInfo.username)
                .Return(u => u.As<User>()).ResultsAsync).FirstOrDefault();

            if (user != null)
            {
                if (user.password == LoginInfo.password)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
