﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Wowthing.Backend.Data;
using Wowthing.Backend.Jobs.NonBlizzard;
using Wowthing.Backend.Models.Data;
using Wowthing.Backend.Models.Data.Achievements;
using Wowthing.Backend.Models.Data.Collections;
using Wowthing.Backend.Models.Data.Covenants;
using Wowthing.Backend.Models.Data.Journal;
using Wowthing.Backend.Models.Data.Professions;
using Wowthing.Backend.Models.Data.Progress;
using Wowthing.Backend.Models.Data.ZoneMaps;
using Wowthing.Backend.Models.Redis;
using Wowthing.Backend.Utilities;
using Wowthing.Lib.Enums;
using Wowthing.Lib.Extensions;
using Wowthing.Lib.Jobs;
using Wowthing.Lib.Models.Wow;
using Wowthing.Lib.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wowthing.Backend.Jobs.Misc
{
    public class CacheStaticJob : JobBase, IScheduledJob
    {
        private JankTimer _timer;
        private IDeserializer _yaml = new DeserializerBuilder()
            .WithNamingConvention(LowerCaseNamingConvention.Instance)
            .Build();

        private Dictionary<int, WowItem> _itemMap;
        private Dictionary<int, WowMount> _mountMap;
        private Dictionary<int, WowPet> _petMap;
        private Dictionary<int, WowToy> _toyMap;

        private Dictionary<Language, Dictionary<int, (int, string)>> _creatureToPet = new();
        private Dictionary<Language, Dictionary<int, (int, string)>> _spellToMount = new();

        private Dictionary<(StringType Type, Language Language, int Id), string> _stringMap;

        public static readonly ScheduledJob Schedule = new ScheduledJob
        {
            Type = JobType.CacheStatic,
            Priority = JobPriority.High,
            Interval = TimeSpan.FromHours(1),
            Version = 31,
        };

        public override async Task Run(params string[] data)
        {
            _timer = new JankTimer();

            await LoadData();

            await BuildStaticData();
            await BuildAchievementData();
            
            Logger.Information("{0}", _timer.ToString());
        }

        private readonly StringType[] _stringTypes = {
            StringType.WowCreatureName,
            StringType.WowItemName,
            StringType.WowMountName,
            StringType.WowSoulbindName,
            StringType.WowSkillLineName,
        };
        private async Task LoadData()
        {
            _itemMap = await Context.WowItem
                .AsNoTracking()
                .ToDictionaryAsync(item => item.Id);

            _mountMap = await Context.WowMount
                .AsNoTracking()
                .ToDictionaryAsync(mount => mount.Id);
            
            _petMap = await Context.WowPet
                .Where(pet => (pet.Flags & 32) == 0)
                .AsNoTracking()
                .ToDictionaryAsync(pet => pet.Id);
            
            _toyMap = await Context.WowToy
                .AsNoTracking()
                .ToDictionaryAsync(toy => toy.Id);
            
            _stringMap = await Context.LanguageString
                .Where(ls => _stringTypes.Contains(ls.Type))
                .AsNoTracking()
                .ToDictionaryAsync(ls => (ls.Type, ls.Language, ls.Id), ls => ls.String);
            
            _timer.AddPoint("Database");

            foreach (var language in Enum.GetValues<Language>())
            {
                _creatureToPet[language] = _petMap.Values.ToDictionary(
                    pet => pet.CreatureId,
                    pet => (pet.Id, _stringMap.GetValueOrDefault((StringType.WowCreatureName, language, pet.CreatureId), "???"))
                );
                
                _spellToMount[language] = _mountMap.Values.ToDictionary(
                    mount => mount.SpellId,
                    mount => (mount.Id, _stringMap.GetValueOrDefault((StringType.WowMountName, language, mount.Id), "???"))
                );
            }
        }
        
        #region Static data
        public async Task BuildStaticData()
        {
            var db = Redis.GetDatabase();

            // RaiderIO
            var raiderIoScoreTiers = await db.JsonGetAsync<Dictionary<int, OutRaiderIoScoreTiers>>(DataRaiderIoScoreTiersJob.CACHE_KEY);
            
            // Currencies
            var currencies = await LoadCurrencies();
            var currencyCategories = await LoadCurrencyCategories();
            _timer.AddPoint("Currencies");

            // Instances
            var instances = await LoadInstances();
            _timer.AddPoint("Instances");

            // Mounts
            var mountSets = LoadSets("mounts");
            AddUncategorized("mounts", _spellToMount[Language.enUS], mountSets);
            _timer.AddPoint("Mounts");

            // Pets
            var petSets = LoadSets("pets");
            AddUncategorized("pets", _creatureToPet[Language.enUS], petSets);
            _timer.AddPoint("Pets");

            var progress = LoadProgress();
            _timer.AddPoint("Progress");
            
            // Professions
            var professions = await LoadProfessions();
            
            // Reputations
            var reputations = await LoadReputations();
            var reputationSets = LoadReputationSets();
            _timer.AddPoint("Reputations");
            
            // Soulbinds
            var soulbinds = await LoadSoulbinds();
            _timer.AddPoint("Soulbinds");
            
            // Talents
            var talents = await LoadTalents();
            _timer.AddPoint("Talents");

            // Toys
            var toySets = LoadSets("toys");
            var itemToToy = _toyMap.Values.ToDictionary(
                toy => toy.ItemId,
                toy => (toy.Id, _stringMap.GetValueOrDefault((StringType.WowItemName, Language.enUS, toy.ItemId), "???"))
            );
            AddUncategorized("toys", itemToToy, toySets);
            _timer.AddPoint("Toys");

            // Zone Maps
            var zoneMaps = await LoadZoneMaps();
            #if DEBUG
            DumpZoneMapQuests(zoneMaps[Language.enUS]);
            #endif
            
            // Basic database dumps
            var realms = await Context.WowRealm.ToListAsync();
            var reputationTiers = new SortedDictionary<int, WowReputationTier>(await Context.WowReputationTier.ToDictionaryAsync(c => c.Id));
            _timer.AddPoint("Database");

            // Ok we're done
            var sortedSpellToMount = new SortedDictionary<int, int>(_spellToMount[Language.enUS].ToDictionary(k => k.Key, v => v.Value.Item1));
            var sortedCreatureToPet = new SortedDictionary<int, int>(_creatureToPet[Language.enUS].ToDictionary(k => k.Key, v => v.Value.Item1));

            string cacheHash = null;
            foreach (var language in Enum.GetValues<Language>())
            {
                Logger.Warning("{Lang}", language);
                
                var cacheData = new RedisStaticCache
                {
                    CurrenciesRaw = currencies,
                    CurrencyCategories = currencyCategories,
                    InstancesRaw = instances,
                    Professions = professions[language],
                    RaiderIoScoreTiers = raiderIoScoreTiers ?? new Dictionary<int, OutRaiderIoScoreTiers>(),
                    RealmsRaw = realms,
                    ReputationsRaw = reputations,
                    ReputationTiers = reputationTiers,
                    Soulbinds = soulbinds[language],
                    Talents = talents,
                    ZoneMapSets = zoneMaps[language],

                    CreatureToPet = sortedCreatureToPet,
                    SpellToMount = sortedSpellToMount,

                    MountSetsRaw = FinalizeCollections(mountSets),
                    PetSetsRaw = FinalizeCollections(petSets),
                    ToySetsRaw = FinalizeCollections(toySets),

                    Progress = progress,
                    ReputationSets = reputationSets,
                };

                var cacheJson = JsonConvert.SerializeObject(cacheData);
                // This ends up being the MD5 of enUS, close enough
                if (cacheHash == null)
                {
                    cacheHash = cacheJson.Md5();
                }

                await db.SetCacheDataAndHash($"static-{language.ToString()}", cacheJson, cacheHash);
            }
            
            _timer.AddPoint("Cache", true);
        }

        private string GetString(StringType type, Language language, int id)
        {
            if (!_stringMap.TryGetValue((type, language, id), out var languageName))
            {
                languageName = _stringMap.GetValueOrDefault(
                    (type, language, id), $"{type.ToString()} #{id}");
            }

            return languageName;
        }

        private List<DataReputationCategory> LoadReputationSets()
        {
            var categories = new List<DataReputationCategory>();

            var basePath = Path.Join(DataUtilities.DataPath, "reputations");
            foreach (var line in File.ReadLines(Path.Join(basePath, "_order")))
            {
                if (line == "-")
                {
                    categories.Add(null);
                }
                else
                {
                    var filePath = Path.Join(basePath, line);
                    categories.Add(_yaml.Deserialize<DataReputationCategory>(File.OpenText(filePath)));
                }
            }

            return categories;
        }

        private static async Task<List<OutCurrency>> LoadCurrencies()
        {
            var types = await DataUtilities.LoadDumpCsvAsync<DumpCurrencyTypes>("currencytypes");
            return types
                .Where(type => !Hardcoded.IgnoredCurrencies.Contains(type.ID))
                .Select(type => new OutCurrency(type))
                .ToList();
        }

        private static async Task<SortedDictionary<int, OutCurrencyCategory>> LoadCurrencyCategories()
        {
            var categories = await DataUtilities.LoadDumpCsvAsync<DumpCurrencyCategory>("currencycategory");
            return new SortedDictionary<int, OutCurrencyCategory>(categories.ToDictionary(k => k.ID, v => new OutCurrencyCategory(v)));
        }

        private async Task<Dictionary<Language, Dictionary<int, OutProfession>>> LoadProfessions()
        {
            var skillLines = await DataUtilities.LoadDumpCsvAsync<DumpSkillLine>(Path.Join("enUS", "skillline"));

            var professions = skillLines
                .Where(line => Hardcoded.PrimaryProfessions.Contains(line.ID) ||
                               Hardcoded.SecondaryProfessions.Contains(line.ID));

            var subProfessions = skillLines
                .Where(line => Hardcoded.PrimaryProfessions.Contains(line.ParentSkillLineID) ||
                               Hardcoded.SecondaryProfessions.Contains(line.ParentSkillLineID))
                .ToGroupedDictionary(line => line.ParentSkillLineID);
            
            var ret = new Dictionary<Language, Dictionary<int, OutProfession>>();
            foreach (var language in Enum.GetValues<Language>())
            {
                ret[language] = professions
                    .ToDictionary(
                        profession => profession.ID,
                        profession => new OutProfession
                        {
                            Id = profession.ID,
                            Name = GetString(StringType.WowSkillLineName, language, profession.ID),
                            Type = Hardcoded.PrimaryProfessions.Contains(profession.ID) ? 0 : 1,
                            SubProfessions = subProfessions
                                .GetValueOrDefault(profession.ID)
                                .EmptyIfNull()
                                .OrderBy(line => line.ParentTierIndex)
                                .Select(line => new OutSubProfession
                                {
                                    Id = line.ID,
                                    Name = GetString(StringType.WowSkillLineName, language, line.ID),
                                })
                                .ToList(),
                        }
                    );
            }
            return ret;
        }

        private static async Task<List<OutReputation>> LoadReputations()
        {
            var factions = await DataUtilities.LoadDumpCsvAsync<DumpFaction>("faction");
            return factions
                .Select(faction => new OutReputation(faction))
                .OrderBy(rep => rep.Id)
                .ToList();
        }

        private async Task<Dictionary<Language, Dictionary<int, List<OutSoulbind>>>> LoadSoulbinds()
        {
            // Load
            var soulbinds = await DataUtilities.LoadDumpCsvAsync<DumpSoulbind>(Path.Join("enUS", "soulbind"));

            var soulbindOrder = (await DataUtilities.LoadDumpCsvAsync<DumpSoulbindUiDisplayInfo>("soulbinduidisplayinfo"))
                .OrderBy(di => di.OrderIndex)
                .Select(di => di.SoulbindID)
                .ToArray();
            
            var talentTreeIds = new HashSet<int>(soulbinds.Select(soulbind => soulbind.GarrTalentTreeID));
            var talents = await DataUtilities.LoadDumpCsvAsync<DumpGarrTalent>(
                "garrtalent",
                (talent) => talentTreeIds.Contains(talent.GarrTalentTreeID)
            );

            var talentIds = new HashSet<int>(talents.Select(talent => talent.ID));
            var talentSpellId = (await DataUtilities.LoadDumpCsvAsync<DumpGarrTalentRank>(
                "garrtalentrank",
                rank => talentIds.Contains(rank.GarrTalentID)
            )).ToDictionary(
                rank => rank.GarrTalentID,
                rank => rank.PerkSpellID
            );

            // Mangle
            var soulbindsByCovenant = soulbinds
                .GroupBy(soulbind => soulbind.CovenantID)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(soulbind => Array.IndexOf(soulbindOrder, soulbind.ID))
                        .ToList()
                    );
            var talentsByTreeId = talents.ToGroupedDictionary(talent => talent.GarrTalentTreeID);

            // Process
            var ret = new Dictionary<Language, Dictionary<int, List<OutSoulbind>>>();

            foreach (var language in Enum.GetValues<Language>())
            {
                ret[language] = new Dictionary<int, List<OutSoulbind>>();

                foreach (var (covenantId, covenantSoulbinds) in soulbindsByCovenant)
                {
                    var retCovenant = ret[language][covenantId] = new List<OutSoulbind>();
                    
                    foreach (var soulbind in covenantSoulbinds)
                    {
                        retCovenant.Add(new OutSoulbind
                        {
                            Id = soulbind.ID,
                            Name = GetString(StringType.WowSoulbindName, language, soulbind.ID),
                            Renown = Hardcoded.SoulbindRenown[soulbind.ID],
                            Rows = talentsByTreeId[soulbind.GarrTalentTreeID]
                                .GroupBy(talent => talent.Tier)
                                .OrderBy(group => group.Key)
                                .Select(group => group
                                    .OrderBy(talent => talent.UiOrder)
                                    .Select(talent => new List<int>
                                    {
                                        talent.UiOrder,
                                        talent.GarrTalentSocketPropertiesID > 0
                                            ? talent.GarrTalentSocketPropertiesID
                                            : talentSpellId[talent.ID],
                                    })
                                    .ToList()
                                )
                                .ToList()
                        });
                    }
                }
            }

            return ret;
        }
        
        private static async Task<Dictionary<int, List<List<int>>>> LoadTalents()
        {
            var talents = await DataUtilities.LoadDumpCsvAsync<DumpTalent>("talent");

            // classId => { tierId => { column => talent } }
            var classTalents = talents
                .Where(talent => talent.ClassID > 0 && talent.SpecID == 0)
                .GroupBy(talent => talent.ClassID)
                .ToDictionary(
                    classGroup => classGroup.Key,
                    classGroup => classGroup
                        .GroupBy(talent => talent.TierID)
                        .ToDictionary(
                            tierGroup => tierGroup.Key,
                            tierGroup => tierGroup
                                .ToDictionary(talent => talent.ColumnIndex)
                        )
                );
            
            // specId => { tierId => { column => talent } }
            var specTalents = talents
                .Where(talent => talent.ClassID > 0 && talent.SpecID > 0)
                .GroupBy(talent => talent.SpecID)
                .ToDictionary(
                    specGroup => specGroup.Key,
                    specGroup => specGroup
                        .GroupBy(talent => talent.TierID)
                        .ToDictionary(
                            tierGroup => tierGroup.Key,
                            tierGroup => tierGroup
                                .ToDictionary(talent => talent.ColumnIndex)
                        )
                );
            
            // specId => classId
            var specToClass = talents
                .Where(talent => talent.ClassID > 0)
                .GroupBy(talent => talent.SpecID)
                .ToDictionary(
                    specGroup => specGroup.Key,
                    specGroup => specGroup.First().ClassID
                );

            var ret = new Dictionary<int, List<List<int>>>();
            foreach (var (specId, tiers) in specTalents)
            {
                var specData = new List<List<int>>();

                for (int tierIndex = 0; tierIndex <= 6; tierIndex++)
                {
                    var tierData = new List<int>();
                    tiers.TryGetValue(tierIndex, out var columns);
                    
                    for (int columnIndex = 0; columnIndex <= 2; columnIndex++)
                    {
                        DumpTalent talent = null;
                        if (columns != null)
                        {
                            columns.TryGetValue(columnIndex, out talent);
                        }
                        
                        if (talent == null)
                        {
                            classTalents[specToClass[specId]][tierIndex].TryGetValue(columnIndex, out talent);
                        }
                        
                        tierData.Add(talent?.SpellID ?? 0);
                    }
                    
                    specData.Add(tierData);
                }

                ret[specId] = specData;
            }

            return ret;
        }
        
        private static readonly HashSet<int> InstanceTypes = new HashSet<int>() {
            1, // Party Dungeon
            2, // Raid Dungeon
        };
        private async Task<List<OutInstance>> LoadInstances()
        {
            var journalInstances = await DataUtilities.LoadDumpCsvAsync<DumpJournalInstance>("journalinstance");
            var mapIdToInstanceId = journalInstances
                .GroupBy(instance => instance.MapID)
                .ToDictionary(k => k.Key, v => v.OrderByDescending(instance => instance.OrderIndex).First().ID);

            var maps = await DataUtilities.LoadDumpCsvAsync<DumpMap>("map");

            var sigh = new Dictionary<int, OutInstance>();
            foreach (var map in maps.Where(m => mapIdToInstanceId.ContainsKey(m.ID) && InstanceTypes.Contains(m.InstanceType)))
            {
                if (mapIdToInstanceId.TryGetValue(map.ID, out int instanceId))
                {
                    if (sigh.ContainsKey(instanceId))
                    {
                        Logger.Information("DUPLICATE BULLSHIT {0}", map.ID, instanceId);
                    }
                    else
                    {
                        sigh.Add(instanceId, new OutInstance(map, instanceId));
                    } 
                }
                else
                {
                    Logger.Information("No mapIdToInstanceId for {0}??", map.ID);
                } 
            }
            return sigh.Values.ToList();
        }

        private List<List<OutProgress>> LoadProgress()
        {
            var ret = new List<List<OutProgress>>();
            
            var progressSets = DataUtilities.LoadData<DataProgress>("progress", Logger);
            foreach (var progressSet in progressSets)
            {
                if (progressSet == null)
                {
                    ret.Add(null);
                    continue;
                }

                ret.Add(progressSet
                    .Select(category => category == null ? null : new OutProgress(category))
                    .ToList()
                );
            }

            return ret;
        }

        private async Task<Dictionary<Language, List<List<OutZoneMapCategory>>>> LoadZoneMaps()
        {
            var zoneMapSets = DataUtilities.LoadData<DataZoneMapCategory>("zone-maps", Logger);

            var ret = new Dictionary<Language, List<List<OutZoneMapCategory>>>();

            foreach (var language in Enum.GetValues<Language>())
            {
                var sets = new List<List<OutZoneMapCategory>>();
                foreach (var catList in zoneMapSets)
                {
                    if (catList == null)
                    {
                        sets.Add(null);
                    }
                    else
                    {
                        sets.Add(catList.Select(cat => cat == null ? null : new OutZoneMapCategory(cat))
                            .ToList());
                    }
                }

                // Change transmog itemId to appearanceId
                var itemModifiedAppearances = await DataUtilities.LoadDumpCsvAsync<DumpItemModifiedAppearance>("itemmodifiedappearance");
                var itemToAppearance = itemModifiedAppearances
                    .GroupBy(r => r.ItemID)
                    .ToDictionary(r => r.Key, r => r.First().ItemAppearanceID);

                foreach (var categories in sets.Where(cats => cats != null))
                {
                    foreach (var category in categories.Where(cat => cat != null))
                    {
                        foreach (var farm in category.Farms)
                        {
                            if (farm.IdType == FarmIdType.Npc && farm.Id > 0)
                            {
                                farm.Name = _stringMap.GetValueOrDefault((StringType.WowCreatureName, language, farm.Id), farm.Name);
                            }
                            
                            foreach (var drop in farm.Drops)
                            {
                                if (drop.Type == "mount")
                                {
                                    drop.Name = _spellToMount[language][drop.Id].Item2;
                                }
                                else if (drop.Type == "pet")
                                {
                                    drop.Name = _creatureToPet[language][drop.Id].Item2;
                                }
                                else if (drop.Type == "toy")
                                {
                                    drop.Name = GetString(StringType.WowItemName, language, drop.Id);
                                }
                                else if (drop.Type == "transmog")
                                {
                                    var dropItem = _itemMap[drop.Id];

                                    drop.Id = itemToAppearance[drop.Id];
                                    drop.Name = GetString(StringType.WowItemName, language, dropItem.Id);

                                    if (dropItem.ClassId == 2)
                                    {
                                        drop.Type = "weapon";
                                        drop.SubType = dropItem.SubclassId;
                                    }
                                    else if (dropItem.ClassId == 4)
                                    {
                                        if (dropItem.SubclassId == 6 || dropItem.InventoryType == WowInventoryType.HeldInOffHand)
                                        {
                                            drop.Type = "weapon";
                                            drop.SubType = dropItem.InventoryType == WowInventoryType.HeldInOffHand ? 30 : 31;
                                        }
                                        else if (dropItem.SubclassId == 5 || dropItem.Flags.HasFlag(WowItemFlags.Cosmetic))
                                        {
                                            drop.Type = "cosmetic";
                                        }
                                        else
                                        {
                                            drop.Type = "armor";
                                            drop.SubType = dropItem.InventoryType == WowInventoryType.Back ? 0 : dropItem.SubclassId;
                                        }
                                    }

                                    drop.ClassMask = dropItem.GetCalculatedClassMask();
                                }
                            }
                        }
                    }
                }

                ret[language] = sets;
            }

            return ret;
        }

        private void DumpZoneMapQuests(List<List<OutZoneMapCategory>> zoneMaps)
        {
            var seenQuests = new HashSet<int>();
            using (var outFile = File.CreateText(Path.Join(DataUtilities.DataPath, "zone-maps", "addon.txt")))
            {
                foreach (var categories in zoneMaps.Where(zm => zm != null))
                {
                    foreach (var category in categories.Where(cat => cat != null))
                    {
                        outFile.WriteLine("    -- Zone Maps: {0}", category.Name);
                        foreach (var farm in category.Farms)
                        {
                            foreach (var questId in farm.QuestIds)
                            {
                                if (questId > 0 && !seenQuests.Contains(questId))
                                {
                                    outFile.WriteLine("    {0}, -- {1}", questId, farm.Name);
                                    seenQuests.Add(questId);
                                }
                            }
                            
                            foreach (var drop in farm.Drops)
                            {
                                foreach (var dropQuestId in drop.QuestIds.EmptyIfNull())
                                {
                                    if (!seenQuests.Contains(dropQuestId))
                                    {
                                        outFile.WriteLine("    {0}, -- {1}:{2}", dropQuestId, farm.Name, drop.Name);
                                        seenQuests.Add(dropQuestId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<List<DataCollectionCategory>> LoadSets(string dirName)
        {
            return DataUtilities.LoadData<DataCollectionCategory>(dirName, Logger);
        }

        private void AddUncategorized(string dirName, Dictionary<int, (int, string)> spellToThing, List<List<DataCollectionCategory>> thingSets)
        {
            var skip = Array.Empty<int>();
            var skipPath = Path.Join(DataUtilities.DataPath, dirName, "_skip.yml");
            if (File.Exists(skipPath))
            {
                var newSkip = _yaml.Deserialize<string[]>(File.OpenText(skipPath));
                if (newSkip != null)
                {
                    skip = newSkip.SelectMany(s => s.Split(' ')).Select(s => int.Parse(s)).ToArray();
                }
            }

            // Lookup keys - things in sets - skip
            var missing = spellToThing.Keys
                .Except(thingSets
                    .Where(s => s != null)
                    .SelectMany(s => s)
                    .Where(c => c.Groups != null)
                    .SelectMany(s => s.Groups)
                    .Where(g => g.Things != null)
                    .SelectMany(g => g.Things)
                    .SelectMany(t => t
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(z => int.Parse(z))
                    )
                )
                .Except(skip)
                .ToArray();

            if (missing.Length > 0)
            {
                thingSets.Add(new List<DataCollectionCategory>{
                    new DataCollectionCategory
                    {
                        Name = "UNCATEGORIZED",
                        Groups = new List<DataCollectionGroup>
                        {
                            new DataCollectionGroup
                            {
                                Name = "UNCATEGORIZED",
                                Things = missing
                                    .Select(m => m.ToString())
                                    .ToList(),
                            },
                        },
                    },
                });
                
#if DEBUG
                using (var file = File.CreateText(Path.Join("..", "..", "data", dirName, "zzz_uncategorized.yml")))
                {
                    foreach (int thing in missing.OrderBy(m => m))
                    {
                        file.WriteLine($"  - {thing} # {spellToThing[thing].Item2}");
                    }
                }
#endif
            }
        }

        private List<List<OutCollectionCategory>> FinalizeCollections(List<List<DataCollectionCategory>> categorySets)
        {
            var ret = new List<List<OutCollectionCategory>>();
            
            foreach (var categorySet in categorySets)
            {
                if (categorySet == null)
                {
                    ret.Add(null);
                    continue;
                }
                
                ret.Add(categorySet
                    .Select(category => new OutCollectionCategory(category))
                    .ToList()
                );
            }

            return ret;
        }
        #endregion
        
        #region Achievement data
        public async Task BuildAchievementData()
        {
            var db = Redis.GetDatabase();

            var achievements = await LoadAchievements();
            var achievementCategories = await LoadAchievementCategories(achievements);
            var achievementCriteria = await LoadAchievementCriteria(achievements);
            
            // Ok we're done
            var cacheData = new RedisStaticAchievements
            {
                Categories = achievementCategories,
                AchievementRaw = achievements.Values.ToList(),
                CriteriaRaw = achievementCriteria.Criteria.Values.ToList(),
                CriteriaTreeRaw = achievementCriteria.CriteriaTree.Values.ToList(),
            };
            var cacheJson = JsonConvert.SerializeObject(cacheData);
            var cacheHash = cacheJson.Md5();
            _timer.AddPoint("JSON");

            await db.SetCacheDataAndHash("achievement", cacheJson, cacheHash);
            _timer.AddPoint("Cache", true);
        }

        private static readonly HashSet<int> SkipAchievementCategories = new()
        {
            1, // Statistics
            15076, // Guild
        };

        private static async Task<List<OutAchievementCategory>> LoadAchievementCategories(Dictionary<int, OutAchievement> achievements)
        {
            var records = await DataUtilities.LoadDumpCsvAsync<DumpAchievementCategory>("achievement_category");
            var outMap = records.ToDictionary(
                record => record.ID,
                record => new OutAchievementCategory(record)
            );

            var achievementMap = achievements.Values
                .GroupBy(a => a.CategoryId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Id).ToList());

            foreach (var record in records)
            {
                // Attach children
                if (record.Parent > -1)
                {
                    outMap[record.Parent].Children.Add(outMap[record.ID]);
                }
                
                // Attach achievements
                if (achievementMap.ContainsKey(record.ID))
                {
                    outMap[record.ID].AchievementIds = achievementMap[record.ID];
                }
            }
            
            // Sort everything by Order
            foreach (var category in outMap.Values)
            {
                category.Children.Sort((a, b) => a.Order.CompareTo(b.Order));
            }

            // Return all root categories that aren't in the skip list
            return outMap.Values
                .Where(record => record.Parent == -1 && !SkipAchievementCategories.Contains(record.Id))
                .OrderBy(record => record.Order)
                .ToList();
        }

        private static async Task<Dictionary<int, OutAchievement>> LoadAchievements()
        {
            var records = await DataUtilities.LoadDumpCsvAsync<DumpAchievement>("achievement");

            var achievementMap = records
                .Where(a => !a.Flags.HasFlag(WowAchievementFlags.Tracking))
                .Select(a => new OutAchievement(a))
                .ToDictionary(a => a.Id);

            foreach (var achievement in achievementMap.Values)
            {
                if (achievement.Supersedes > 0 && achievementMap.ContainsKey(achievement.Supersedes))
                {
                    achievementMap[achievement.Supersedes].SupersededBy = achievement.Id;
                }
            }

            return achievementMap;
        }

        private static async Task<AchievementCriteria> LoadAchievementCriteria(Dictionary<int, OutAchievement> achievements)
        {
            var criteria = await DataUtilities.LoadDumpCsvAsync<DumpCriteria>("criteria");
            //var criteriaMap = criteria.ToDictionary(c => c.ID);
            
            var criteriaTrees = await DataUtilities.LoadDumpCsvAsync<DumpCriteriaTree>("criteriatree");
            var criteriaTreeMap = criteriaTrees.ToDictionary(ct => ct.ID);
            
            //var modifierTrees = await CsvUtilities.LoadDumpCsvAsync<DumpModifierTree>("modifiertree");
            //var modifierTreeMap = modifierTrees.ToDictionary(mt => mt.ID);

            // Keep track of CriteriaTree tree 
            foreach (var criteriaTree in criteriaTrees.Where(ct => ct.Parent > 0))
            {
                if (criteriaTreeMap.TryGetValue(criteriaTree.Parent, out var parent))
                {
                    parent.Children.Add(criteriaTree);
                }
            }
            
            // Filter things
            var achievementCriteriaTrees = new HashSet<int>(achievements.Values.Select(a => a.CriteriaTreeId));
            var filtered = criteriaTrees
                .Where(ct => achievementCriteriaTrees.Contains(ct.ID));
            var final = filtered
                .Concat(
                    filtered
                        .SelectManyRecursive(ct => ct.Children)
                )
                .OrderBy(ct => ct.ID);
            
            // Outputs
            return new AchievementCriteria
            {
                Criteria = criteria.Select(c => new OutCriteria(c)).ToDictionary(c => c.Id),
                //CriteriaTree = criteriaTrees.Select(ct => new OutCriteriaTree(ct, criteriaTreeChildren)).ToDictionary(ct => ct.Id),
                CriteriaTree = final.Select(ct => new OutCriteriaTree(ct)).ToDictionary(ct => ct.Id)
            };
        }
        #endregion
    }

    internal struct AchievementCriteria
    {
        public Dictionary<int, OutCriteria> Criteria;
        public Dictionary<int, OutCriteriaTree> CriteriaTree;
    }
}
