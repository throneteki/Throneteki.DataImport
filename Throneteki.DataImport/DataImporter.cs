namespace CrimsonDev.Throneteki.DataImport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CrimsonDev.Throneteki.Data;
    using CrimsonDev.Throneteki.Data.GameData;
    using CrimsonDev.Throneteki.DataImport.Models;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    public class DataImporter
    {
        private readonly ThronetekiDbContext context;
        private Dictionary<string, int> factions;

        public DataImporter(ThronetekiDbContext context)
        {
            this.context = context;
        }

        public async Task ImportAsync(string path)
        {
            var packs = Directory.GetFiles(Path.Combine(path, "packs"), "*.json").Select(file => JsonConvert.DeserializeObject<JsonPack>(File.ReadAllText(file))).ToList();
            factions = (await context.Faction.ToListAsync()).ToDictionary(key => key.Code, value => value.Id);
            var allCards = new List<Card>();
            var cardsByCode = new Dictionary<string, Card>();

            foreach (var pack in packs)
            {
                Console.WriteLine($"Processing Pack {pack.Name}");
                var dbPack = await context.Pack.SingleOrDefaultAsync(p => p.Code == pack.Code) ?? new Pack
                {
                    Code = pack.Code
                };

                UpdatePack(dbPack, pack);

                if (dbPack.Id == 0)
                {
                    context.Pack.Add(dbPack);
                }

                foreach (var card in pack.Cards)
                {
                    var dbCard = await context.Card.SingleOrDefaultAsync(c => c.Code == card.Code) ?? new Card
                    {
                        Code = card.Code
                    };
                    UpdateCard(dbCard, dbPack, card);

                    if (dbCard.Id == 0)
                    {
                        context.Card.Add(dbCard);
                    }

                    allCards.Add(dbCard);
                }

                Console.WriteLine($"{pack.Cards.Count} cards processed");
            }

            foreach (var card in allCards)
            {
                // If there's more than one card with the same name, put the pack code in the label
                card.Label = allCards.Count(c => c.Name == card.Name) > 1 ? $"{card.Name} ({card.Pack.Code})" : card.Name;
                cardsByCode.Add(card.Code, card);
            }

            await ImportRestrictedList(path, cardsByCode);

            await context.SaveChangesAsync();
        }

        private static void UpdatePack(Pack dbPack, JsonPack pack)
        {
            dbPack.Name = pack.Name;
            dbPack.ReleaseDate = pack.ReleaseDate;
        }

        private async Task ImportRestrictedList(string path, Dictionary<string, Card> cardsByCode)
        {
            var restrictedList = JsonConvert.DeserializeObject<List<JsonRestrictedListEntry>>(File.ReadAllText(Path.Join(path, "restricted-list.json")));

            Console.WriteLine("Importing restricted list");

            foreach (var entry in restrictedList)
            {
                var dbEntry =
                    await context.RestrictedListEntry.Include(rl => rl.MeleeCards).Include(rl => rl.JoustCards).Include("MeleeCards.Card")
                        .Include("JoustCards.Card").SingleOrDefaultAsync(r => r.Version == entry.Version)
                    ?? new RestrictedListEntry { Version = entry.Version };

                dbEntry.Date = entry.Date;

                if (dbEntry.Id == 0)
                {
                    context.RestrictedListEntry.Add(dbEntry);
                }

                foreach (var joustCard in entry.JoustCards)
                {
                    if (dbEntry.JoustCards.Any(c => c.Card.Code == joustCard))
                    {
                        continue;
                    }

                    var card = cardsByCode[joustCard];
                    var newJoustCard = new RestrictedListJoustCard { RestrictedListEntry = dbEntry, Card = card };

                    dbEntry.JoustCards.Add(newJoustCard);
                }

                foreach (var meleeCard in entry.MeleeCards)
                {
                    var card = cardsByCode[meleeCard];

                    if (dbEntry.MeleeCards.Any(c => c.CardId == card.Id))
                    {
                        continue;
                    }

                    var newMeleeCard = new RestrictedListMeleeCard { RestrictedListEntryId = dbEntry.Id, CardId = card.Id };

                    dbEntry.MeleeCards.Add(newMeleeCard);
                }

                await context.SaveChangesAsync();
            }
        }

        private void UpdateCard(Card dbCard, Pack dbPack, JsonCard card)
        {
            dbCard.Pack = dbPack;
            dbCard.Type = card.Type;
            dbCard.DeckLimit = card.DeckLimit;
            dbCard.Name = card.Name;
            dbCard.Loyal = card.Loyal;
            dbCard.Text = card.Text;
            dbCard.Unique = card.Unique;
            dbCard.Cost = card.Cost;
            dbCard.Strength = card.Strength;
            dbCard.FactionId = factions[card.Faction];
            dbCard.Traits = string.Join(separator: ',', values: card.Traits);
            dbCard.Icons = card.Icons ?? new CardIcons();
            dbCard.PlotStats = card.PlotStats ?? new PlotStats();
        }
    }
}
