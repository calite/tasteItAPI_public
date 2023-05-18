using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Neo4jClient;
using System.Globalization;
using System.Text;
using TasteItApi.Models;


namespace TasteItApi.Controllers
{    

    public class PublicController : Controller
    {
        private readonly IGraphClient _client;

        //DOC: https://github.com/DotNet4Neo4j/Neo4jClient/wiki

        public PublicController(IGraphClient client)
        {
            _client = client;
        }

        //devuelve todas las recetas
        [HttpGet("/public/recipes/all")]
        public async Task<ActionResult<Recipe>> GetAllRecipes()
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                    .OrderBy("recipe.dateCreated desc")
                .ResultsAsync;

                var results = result.ToList();

                if(results.Count > 0)
                {
                    return Ok(results);
                }
                else
                {
                    return NotFound();
                }
            
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
            

        }

        //devuelve las recetas con paginacion
        [HttpGet("/public/recipes/all/{skipper:int}")]
        public async Task<ActionResult<Recipe>> GetAllRecipesWithSkipper(int skipper)
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>(),
                    })
                    .OrderBy("recipe.dateCreated desc")
                    .Skip(skipper)
                    .Limit(10)
                .ResultsAsync;

                var results = result.ToList();

                if(results.Count > 0)
                {
                    return Ok(results);
                }else
                {
                    return NotFound();
                }  

            } catch(Exception ex)
            {
                return BadRequest(ex);
            }

        }

        //devuelve una receta a partir de un id determinado
        [HttpGet("/public/recipes/{id:int}")]
        public async Task<ActionResult<Recipe>> GetRecipeById(int id)
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Where("ID(recipe) = " + id)
                    .Return((recipe, user) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                .ResultsAsync;

                if (result.ToList().Count == 0)
                {
                    return NotFound();
                }

                return Ok(result);

            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

        //devuelve un numero aleatorio de recetas
        [HttpGet("/public/recipes/random/{limit}")]
        public async Task<ActionResult<Recipe>> GetRandomRecipesWithLimit(int limit)
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .With("recipe, rand() as rand")
                    .OrderBy("rand limit $limit")
                    .Match("(recipe:Recipe)")
                    .WithParam("limit", limit)
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                .ResultsAsync;

                var results = result.ToList();

                if(results.Count > 0)
                {
                    return Ok(results);
                }
                else
                {
                    return BadRequest();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        //devuelve recetas filtrando por nombre
        [HttpGet("/public/recipes/byname/{name}/{skipper}")]
        public async Task<ActionResult<Recipe>> GetRecipeByName(string name, int skipper)
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)-[:Created]-(user:User)")
                    .Where("toLower(recipe.name) CONTAINS toLower($name)")
                    .WithParam("name", name)
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                    .OrderBy("recipe.dateCreated desc")
                    .Skip(skipper)
                    .Limit(10)
                .ResultsAsync;

                var results = result.ToList();

                if (results.Count > 0)
                {
                    return Ok(results);
                }
                else
                {
                    return NotFound();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        //filtramos recetas filtradas por ciudad
        [HttpGet("/public/recipes/bycountry/{country}/{skipper}")]
        public async Task<ActionResult<Recipe>> GetRecipeByCountry(string country, int skipper)
        {
            try
            {
                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Where("toLower(recipe.country) CONTAINS toLower($country)")
                    .WithParam("country", country)
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                   .OrderBy("recipe.dateCreated desc")
                   .Skip(skipper)
                   .Limit(10)
               .ResultsAsync;

                var results = result.ToList();

                if(results.Count > 0)
                {
                    return Ok(results);
                } else
                {
                    return NotFound();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        //devuelve recetas filtradas por ingredientes, acepta ingredientes separados por comas
        [HttpGet("/public/recipes/byingredients/{ingredients}/{skipper}")]
        public async Task<ActionResult<List<Recipe>>> GetRecipeByIngredients(string ingredients, int skipper)
        {
            try
            {
                List<string> listIng = ingredients.Replace(" ", "").Split(",").ToList();

                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>(),
                    })
                    .OrderBy("recipe.dateCreated desc")
                .ResultsAsync;

                var recipes = result.ToList();

                List<Object> listRecipesFiltered = new List<Object>();

                for (int i = 0; i < recipes.Count; i++)
                {
                    //miramos si la lista de los ingredientes contiene alguno de los elementos de los introducidos por el usuario
                    bool hasMatch = recipes[i].Recipe.ingredients.Any(x => listIng.Any(y => x.ToLower().Contains(y.ToLower())));

                    if (hasMatch)
                    {
                        listRecipesFiltered.Add(new
                        {
                            recipes[i].RecipeId,
                            Recipe = recipes[i].Recipe.As<Recipe>()
                        });
                    }

                }     

                if(listRecipesFiltered.Count > 0)
                {
                    return Ok(listRecipesFiltered.ToList());
                }
                else
                {
                    return NotFound();
                }

            } catch(Exception ex)
            {
                return BadRequest(ex);
            }

        }

        //devuelve recetas filtradas por tags, acepta tags separados por comas
        [HttpGet("/public/recipes/bytags/{tags}/{skipper}")]
        public async Task<ActionResult<List<Recipe>>> GetRecipeByTags(string tags, int skipper)
        {
            try
            {
                List<string> listTags = tags.Replace(" ", "").Split(",").ToList();

                var result = await _client.Cypher
                    .Match("(recipe:Recipe)")
                    .Return((recipe) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                    .OrderBy("recipe.dateCreated desc")
                .ResultsAsync;

                var recipes = result.ToList();

                List<Object> listRecipesFiltered = new List<Object>();

                for (int i = 0; i < recipes.Count; i++)
                {
                    //miramos si la lista de los ingredientes contiene alguno de los elementos de los introducidos por el usuario
                    bool hasMatch = recipes[i].Recipe.tags.Any(x => listTags.Any(y => y.ToLower() == x.ToLower()));

                    if (hasMatch)
                    {
                        listRecipesFiltered.Add(new
                        {
                            recipes[i].RecipeId,
                            Recipe = recipes[i].Recipe.As<Recipe>()
                        });
                    }

                }

                if (listRecipesFiltered.Count > 0)
                {
                    return Ok(listRecipesFiltered.ToList());
                }
                else
                {
                    return NotFound();
                }

            } catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

        //buscador de recetas, contiene varios filtros, acepta o no los parametros
        [HttpGet("/public/recipes/search")]
        public async Task<ActionResult<Recipe>> GetRecipesFiltered(string? name, string? country, int? difficulty, int? rating, string? ingredients, string? tags)
        {
            try
            {
                List<string> listIng = new List<string>();
                List<string> listTags = new List<string>();

                if (ingredients != null)
                {
                    listIng = ingredients.Replace(" ", "").Split(",").ToList();
                }

                if (tags != null)
                {
                    listTags = tags.Replace(" ", "").Split(",").ToList();
                }

                var result = await _client.Cypher
                    .Match("(recipe:Recipe)-[c:Created]-(user:User)")
                    .Where("($name IS NULL OR toLower(recipe.name) CONTAINS toLower($name))")
                    .AndWhere("($country IS NULL OR toLower(recipe.country) CONTAINS toLower($country))")
                    .AndWhere("($difficulty IS NULL OR recipe.difficulty = $difficulty)")
                    .AndWhere("($rating IS NULL OR recipe.rating = $rating)")
                    .WithParam("name", name)
                    .WithParam("country", country)
                    .WithParam("difficulty", difficulty)
                    .WithParam("rating", rating)
                    .Return((recipe, user) => new
                    {
                        RecipeId = recipe.Id(),
                        Recipe = recipe.As<Recipe>()
                    })
                    .OrderBy("recipe.dateCreated desc")
                    .ResultsAsync;

                var recipes = result.ToList();

                List<Object> listRecipesFiltered = new List<Object>();

                if (listIng.Count > 0 && listTags.Count == 0) // Filtrar solo por ingredientes
                {
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        bool hasMatch = recipes[i].Recipe.ingredients.Any(x => listIng.Any(y => x.ToLower().Contains(y.ToLower())));

                        if (hasMatch)
                        {
                            listRecipesFiltered.Add(new
                            {
                                recipes[i].RecipeId,
                                Recipe = recipes[i].Recipe.As<Recipe>()
                            });
                        }
                    }
                }
                else if (listTags.Count > 0 && listIng.Count == 0) // Filtrar solo por tags
                {
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        bool hasMatch = recipes[i].Recipe.tags.Any(x => listTags.Any(y => x.ToLower().Contains(y.ToLower())));

                        if (hasMatch)
                        {
                            listRecipesFiltered.Add(new
                            {
                                recipes[i].RecipeId,
                                Recipe = recipes[i].Recipe.As<Recipe>(),
                            });
                        }
                    }
                }
                else if (listIng.Count > 0 && listTags.Count > 0) // Filtrar por ingredientes y tags
                {
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        bool hasIngredientMatch = recipes[i].Recipe.ingredients.Any(x => listIng.Any(y => x.ToLower().Contains(y.ToLower())));
                        bool hasTagMatch = recipes[i].Recipe.tags.Any(x => listTags.Any(y => x.ToLower().Contains(y.ToLower())));

                        if (hasIngredientMatch && hasTagMatch)
                        {
                            listRecipesFiltered.Add(new
                            {
                                RecipeId = recipes[i].RecipeId,
                                Recipe = recipes[i].Recipe.As<Recipe>(),
                            });
                        }
                    }
                }

                if(recipes.Count == 0)
                {
                    return NotFound();
                }

                if (listRecipesFiltered.Count > 0)
                {
                    return Ok(listRecipesFiltered);
                }
                else
                {
                    return Ok(recipes);
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

    }
}
