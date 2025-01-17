import flatten from 'lodash/flatten'
import some from 'lodash/some'
import { get } from 'svelte/store'

import { Constants } from './constants'
import { userQuestStore } from '@/stores'
import { staticStore } from '@/shared/stores/static'
import { Profession } from '@/enums/profession'
import type { Character } from '@/types'
import type { Chore, Task } from '@/types/tasks'
import { dragonflightProfessions, isGatheringProfession, professionSlugToId } from './professions'


export const dragonflightProfessionTasks: Chore[] = flatten(
    dragonflightProfessions.map((profession) => {
        const name = Profession[profession.id]
        const lowerName = Profession[profession.id].toLowerCase()
        const tasks: Chore[] = []

        tasks.push(
            {
                taskKey: `dfProfession${name}Provide`,
                taskName: `${name}: Provide`,
                minimumLevel: 60,
                couldGetFunc: (char) => couldGet(lowerName, char),
                canGetFunc: (char) => getLatestSkill(char, lowerName, 45),
            },
        )

        if (profession.hasTask === true) {
            tasks.push(
                {
                    taskKey: `dfProfession${name}Task`,
                    taskName: `${name}: Task`,
                    minimumLevel: 60,
                    couldGetFunc: (char) => couldGet(lowerName, char),
                    canGetFunc: (char) => getLatestSkill(char, lowerName,
                        isGatheringProfession[profession.id] ? 45 : 25),
                },
            )
        }

        tasks.push(
            {
                taskKey: `dfProfession${name}Drop#`,
                taskName: `${name}: Drops`,
                minimumLevel: 60,
                couldGetFunc: (char) => couldGet(lowerName, char),
                //canGetFunc: (char) => getLatestSkill(char, lowerName, 45),
            },
        )

        if (profession.hasOrders === true) {
            tasks.push(
                {
                    taskKey: `dfProfession${name}Orders`,
                    taskName: `${name}: Orders`,
                    minimumLevel: 60,
                    couldGetFunc: (char) => couldGet(lowerName, char),
                    canGetFunc: (char) => getLatestSkill(char, lowerName, 25),
                },
            )
        }

        tasks.push(
            {
                taskKey: `dfProfession${name}Treatise`,
                taskName: `${name}: Treatise`,
                minimumLevel: 60,
                couldGetFunc: (char) => couldGet(lowerName, char),
            },
        )

        return tasks
    })
)

export const taskList: Task[] = [
    // Events/Holidays/idk
    {
        key: 'holidayDarkmoonFaire',
        name: '[Event] Darkmoon Faire',
        shortName: 'DMF',
        type: 'multi',
    },
    {
        key: 'holidayWinterVeil',
        minimumLevel: 30,
        name: '[Event] Winter Veil',
        shortName: 'Xmas',
        type: 'multi',
    },

    // Weekly Holidays
    {
        key: 'holidayArena',
        name: '[Event] Arena Skirmishes',
        shortName: 'Arena',
    },
    {
        key: 'holidayBattlegrounds',
        name: '[Event] Battlegrounds',
        shortName: 'BGs',
    },
    {
        key: 'holidayDungeons',
        name: '[Event] Mythic Dungeons',
        shortName: 'MDun',
    },
    {
        key: 'holidayPetPvp',
        name: '[Event] Pet PvP',
        shortName: 'Pets',
    },
    {
        key: 'holidayTimewalking',
        name: '[Event] Timewalking Dungeons',
        shortName: 'TW :exclamation:',
    },
    {
        key: 'holidayTimewalkingItem',
        name: '[Event] Timewalking Item',
        shortName: 'TW :item:',
        minimumLevel: Constants.characterMaxLevel - 10,
    },
    {
        key: 'holidayWorldQuests',
        name: '[Event] World Quests',
        shortName: 'WQs',
    },

    // PvP
    {
        key: 'pvpOverwhelmingOdds',
        name: '[PvP] Overwhelming Odds',
        shortName: 'WM',
    },
    {
        key: 'pvpBlitz',
        minimumLevel: 70,
        name: '[PvP] Battleground Blitz',
        shortName: 'Blitz',
        type: 'multi',
    },
    {
        key: 'pvpBrawl',
        name: '[PvP] Brawl',
        shortName: 'Brawl',
    },
    {
        key: 'pvpSkirmishes',
        name: '[PvP] Arena Skirmishes',
        shortName: 'Skirm',
    },
    {
        key: 'pvpWar',
        name: '[PvP] Epic Battlegrounds',
        shortName: 'EBGs',
    },
    {
        key: 'pvpBattle',
        name: '[PvP] Random Battlegrounds',
        shortName: 'BGs',
    },
    {
        key: 'pvpArenas',
        name: '[PvP] Rated Arenas',
        shortName: 'RA',
    },
    {
        key: 'pvpTeamwork',
        name: '[PvP] Rated Battlegrounds',
        shortName: 'RBGs',
    },
    {
        key: 'pvpSolo',
        name: '[PvP] Solo Shuffle',
        shortName: 'Solo',
    },

    // Warlords of Draenor
    {
        key: 'wodGarrison',
        name: '[WoD] Garrison Invasions',
        shortName: 'GInv',
        type: 'multi',
        minimumLevel: 10,
    },

    // Legion
    {
        key: 'legionWitheredTraining',
        name: '[Legion] Withered Army Training',
        shortName: 'Wither',
        minimumLevel: 45,
        requiredQuestId: 44636, // Building an Army
    },

    // Shadowlands
    {
        key: 'slAnima',
        name: '[SL] Anima',
        shortName: 'Anima',
        minimumLevel: 60,
    },
    {
        key: 'slShapingFate',
        name: '[SL] Shaping Fate (Korthia)',
        shortName: 'Korth',
        minimumLevel: 60,
    },
    {
        key: 'slMawAssault',
        name: '[SL] Maw Assault',
        shortName: 'Maw ⚔',
        minimumLevel: 60,
    },
    {
        key: 'slTormentors',
        name: '[SL] Tormentors of Torghast',
        shortName: 'Torm',
        minimumLevel: 60,
    },
    {
        key: 'slPatterns',
        name: '[SL] Patterns (Zereth Mortis)',
        shortName: 'ZM',
        minimumLevel: 60,
    },

    // Dragonflight
    {
        key: 'dfAidingAccord',
        name: '[DF] Aiding the Accord',
        shortName: 'AtA',
        minimumLevel: 60,
    },
    {
        key: 'dfWorthyAllyLoammNiffen',
        name: '[DF] A Worthy Ally: Loamm Niffen',
        shortName: 'WA:LN',
        minimumLevel: 70,
    },
    {
        key: 'dfCatchRelease',
        name: '[DF] Catch and Release (Fishing)',
        shortName: 'CaR',
        type: 'multi',
    },
    {
        key: 'dfChores',
        name: '[DF] Chores - 10.0.x',
        shortName: '10.0',
        minimumLevel: 60,
        type: 'multi',
    },
    {
        key: 'dfChores10_1_0',
        name: '[DF] Chores - 10.1.x',
        shortName: '10.1',
        minimumLevel: 60,
        type: 'multi',
    },
    {
        key: 'dfChores10_2_0',
        name: '[DF] Chores - 10.2.x',
        shortName: '10.2',
        minimumLevel: 70,
        type: 'multi',
    },
    {
        key: 'dfProfessionWeeklies',
        name: '[DF] Profession Weeklies',
        shortName: 'Pro',
        type: 'multi',
        minimumLevel: 60,
    },
    {
        key: 'dfSparks',
        name: '[DF] Sparks of Life (PvP)',
        shortName: 'SoL',
        minimumLevel: 60,
    },
    {
        key: 'dfTimeRift',
        name: '[DF] Time Rifts',
        shortName: 'TR',
        minimumLevel: 60,
    },
]

export const taskMap: Record<string, Task> = Object.fromEntries(
    taskList.map((task) => [task.key, task])
)


function garrisonCouldGet(char: Character): boolean {
    return userQuestStore.hasAny(char.id, 36592) || userQuestStore.hasAny(char.id, 36567)
}

function winterVeilCouldGet(char: Character): boolean {
    return userQuestStore.hasAny(char.id, 36615) || userQuestStore.hasAny(char.id, 36614)
}

export const multiTaskMap: Record<string, Chore[]> = {
    'holidayDarkmoonFaire': [
        {
            minimumLevel: 1,
            taskKey: 'dmfAlchemy',
            taskName: ':alchemy: A Fizzy Fusion',
            couldGetFunc: (char) => !!char.professions?.[Profession.Alchemy],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfBlacksmithing',
            taskName: ':blacksmithing: Baby Needs Two Pair of Shoes',
            couldGetFunc: (char) => !!char.professions?.[Profession.Blacksmithing],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfEnchanting',
            taskName: ':enchanting: Putting Trash to Good Use',
            couldGetFunc: (char) => !!char.professions?.[Profession.Enchanting],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfEngineering',
            taskName: ":engineering: Talkin' Tonks",
            couldGetFunc: (char) => !!char.professions?.[Profession.Engineering],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfHerbalism',
            taskName: ':herbalism: Herbs for Healing',
            couldGetFunc: (char) => !!char.professions?.[Profession.Herbalism],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfInscription',
            taskName: ':inscription: Writing the Future',
            couldGetFunc: (char) => !!char.professions?.[Profession.Inscription],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfJewelcrafting',
            taskName: ':jewelcrafting: Keeping the Faire Sparkling',
            couldGetFunc: (char) => !!char.professions?.[Profession.Jewelcrafting],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfLeatherworking',
            taskName: ':leatherworking: Eyes on the Prizes',
            couldGetFunc: (char) => !!char.professions?.[Profession.Leatherworking],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfMining',
            taskName: ':mining: Rearm, Reuse, Recycle',
            couldGetFunc: (char) => !!char.professions?.[Profession.Mining],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfSkinning',
            taskName: ':skinning: Tan My Hide',
            couldGetFunc: (char) => !!char.professions?.[Profession.Skinning],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfTailoring',
            taskName: ':tailoring: Banners, Banners Everywhere!',
            couldGetFunc: (char) => !!char.professions?.[Profession.Tailoring],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfArchaeology',
            taskName: ':archaeology: Fun for the Little Ones',
            couldGetFunc: (char) => !!char.professions?.[Profession.Archaeology],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfCooking',
            taskName: ':cooking: Putting the Crunch in the Frog',
            couldGetFunc: (char) => !!char.professions?.[Profession.Cooking],
        },
        {
            minimumLevel: 1,
            taskKey: 'dmfFishing',
            taskName: ":fishing: Spoilin' for Salty Sea Dogs",
            couldGetFunc: (char) => !!char.professions?.[Profession.Fishing],
        },
    ],
    'holidayWinterVeil': [
        {
            minimumLevel: 30,
            taskKey: 'meanOne',
            taskName: "...You're a Mean One",
        },
        {
            minimumLevel: 40,
            taskKey: 'merryGrumpus',
            taskName: 'Grumpus',
            couldGetFunc: winterVeilCouldGet,
        },
        {
            minimumLevel: 40,
            taskKey: 'merryGrumplings',
            taskName: 'Menacing Grumplings',
            couldGetFunc: winterVeilCouldGet,
        },
        {
            minimumLevel: 40,
            taskKey: 'merryPresents',
            taskName: 'What Horrible Presents!',
            couldGetFunc: winterVeilCouldGet,
        },
        {
            minimumLevel: 40,
            taskKey: 'merryChildren',
            taskName: 'Where Are the Children?',
            couldGetFunc: winterVeilCouldGet,
        },
    ],
    'wodGarrison': [
        {
            taskKey: 'invasionBronze',
            taskName: '{item:120320}', // Invader's Abandoned Sack
            couldGetFunc: garrisonCouldGet,
            minimumLevel: 10,
        },
        {
            taskKey: 'invasionSilver',
            taskName: '{item:120319}', // Invader's Damaged Cache
            couldGetFunc: garrisonCouldGet,
            minimumLevel: 10,
        },
        {
            taskKey: 'invasionGold',
            taskName: '{item:116980}', // Invader's Forgotten Treasure
            couldGetFunc: garrisonCouldGet,
            minimumLevel: 10,
        },
        {
            taskKey: 'invasionPlatinum',
            taskName: '{item:122163}', // Routed Invader's Crate of Spoils
            couldGetFunc: garrisonCouldGet,
            minimumLevel: 10,
        },
    ],
    'dfCatchRelease': [
        {
            taskKey: 'dfCatchAileron',
            taskName: 'Aileron Seamoth',
        },
        {
            taskKey: 'dfCatchCerulean',
            taskName: 'Cerulean Spinefish',
        },
        {
            taskKey: 'dfCatchIslefin',
            taskName: 'Islefin Dorado',
        },
        {
            taskKey: 'dfCatchScalebelly',
            taskName: 'Scalebelly Mackerel',
        },
        {
            taskKey: 'dfCatchTemporal',
            taskName: 'Temporal Dragonhead',
        },
        {
            taskKey: 'dfCatchThousandbite',
            taskName: 'Thousandbite Piranha',
        },
    ],
    'dfChores': [
        {
            minimumLevel: 60,
            taskKey: 'dfCommunityFeast',
            taskName: 'Community Feast',
        },
        // { // actually daily
        //     taskKey: 'dfCommunityFeastKill',
        //     taskName: 'Community Feast: Boss',
        // },
        {
            taskKey: 'dfDragonAllegiance',
            taskName: 'Dragon selected',
        },
        {
            taskKey: 'dfDragonKey',
            taskName: 'Dragon key turned in',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfGrandHuntMythic',
            taskName: 'Grand Hunt: Epic',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfGrandHuntRare',
            taskName: 'Grand Hunt: Rare',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfGrandHuntUncommon',
            taskName: 'Grand Hunt: Uncommon',
        },
        {
            taskKey: 'dfPrimalStorm',
            taskName: 'Primal Storm: Air',
        },
        {
            taskKey: 'dfPrimalEarth',
            taskName: 'Primal Storm: Earth',
        },
        {
            taskKey: 'dfPrimalFire',
            taskName: 'Primal Storm: Fire',
        },
        {
            taskKey: 'dfPrimalWater',
            taskName: 'Primal Storm: Water',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfSiegeDragonbaneKeep',
            taskName: 'Siege on Dragonbane Keep',
        },
        {
            taskKey: 'dfStormsFury',
            taskName: "Storm's Fury",
        },
        {
            minimumLevel: 60,
            taskKey: 'dfTrialElements',
            taskName: 'Trial of Elements',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfTrialFlood',
            taskName: 'Trial of the Flood',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfReachStormsChest',
            taskName: '[FR] Chest of Storms',
        },
    ],
    'dfChores10_1_0': [
        {
            taskKey: 'dfDreamsurge',
            taskName: 'Dreamsurge',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfFyrakkAssault',
            taskName: 'Fyrakk - Assault',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfFyrakkDisciple',
            taskName: 'Fyrakk - Disciple',
        },
        {
            minimumLevel: 60,
            taskKey: 'dfFyrakkShipment',
            taskName: 'Fyrakk - Secured Shipment',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfResearchersUnderFire1',
            taskName: 'Researchers Under Fire :quality-1-T1:',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfResearchersUnderFire2',
            taskName: 'Researchers Under Fire :quality-2-T2:',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfResearchersUnderFire3',
            taskName: 'Researchers Under Fire :quality-3-T3:',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfResearchersUnderFire4',
            taskName: 'Researchers Under Fire :quality-4-T4:',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfSniffenDig1',
            taskName: 'Sniffenseeking - Dig 1',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfSniffenDig2',
            taskName: 'Sniffenseeking - Dig 2',
        },
        {
            minimumLevel: 70,
            taskKey: 'dfSniffenDig3',
            taskName: 'Sniffenseeking - Dig 3',
        },
    ],
    'dfChores10_2_0': [
        {
            taskKey: 'dfWorthyAllyDreamWardens',
            taskName: 'A Worthy Ally: Dream Wardens',
        },
        {
            taskKey: 'dfBloomingDreamseeds',
            taskName: 'Blooming Dreamseeds',
        },
        {
            taskKey: 'dfGoodsShipments1',
            taskName: 'Shipments x1'
        },
        {
            taskKey: 'dfGoodsShipments5',
            taskName: 'Shipments x5'
        },
        {
            taskKey: 'dfSuperbloom',
            taskName: 'Superbloom',
        },
    ],
    'dfDungeonWeeklies': [
        {
            taskKey: 'dfDungeonPreserving',
            taskName: 'Preserving the Past'
        },
        {
            taskKey: 'dfDungeonRelic',
            taskName: 'Relic Recovery',
        },
    ],
    'dfProfessionWeeklies': [
        {
            taskKey: 'dfProfessionMettle',
            taskName: 'Show Your Mettle',
            minimumLevel: 60,
            couldGetFunc: (char) => some(
                Object.values(get(staticStore).professions).filter((prof) => prof.type === 0),
                (profession) => !!char.professions?.[profession.id]?.[profession.subProfessions[9].id]
            ),
            canGetFunc: (char) => char.reputations?.[2544] >= 500 ? '' : "Need Preferred with Artisan's Consortium",
        },
        ...dragonflightProfessionTasks,
    ],
    'pvpBrawl': [
        {
            taskKey: 'arathiBlizzard',
            taskName: 'Arathi Blizzard',
        },
        {
            taskKey: 'classicAshran',
            taskName: 'Classic Ashran',
        },
        {
            taskKey: 'compStomp',
            taskName: 'Comp Stomp',
        },
        {
            taskKey: 'cookingImpossible',
            taskName: 'Cooking Impossible',
        },
        {
            taskKey: 'deepSix',
            taskName: 'Deep Six',
        },
        {
            taskKey: 'deepwindDunk',
            taskName: 'Deepwind Dunk',
        },
        {
            taskKey: 'gravityLapse',
            taskName: 'Gravity Lapse',
        },
        {
            taskKey: 'packedHouse',
            taskName: 'Packed House',
        },
        {
            taskKey: 'shadoPanShowdown',
            taskName: 'Shado-Pan Showdown',
        },
        {
            taskKey: 'southshoreVsTarrenMill',
            taskName: 'Southshore vs. Tarren Mill',
        },
        {
            taskKey: 'templeOfHotmogu',
            taskName: 'Temple of Hotmogu',
        },
        {
            taskKey: 'warsongScramble',
            taskName: 'Warsong Scramble',
        },
    ],
    'pvpBlitz': [
        {
            taskKey: 'pvpBlitz1',
            taskName: 'Gotta Go Fast',
        },
        {
            taskKey: 'pvpBlitz3',
            taskName: 'Gotta Go Faster',
        },
    ],
}

export const pvpBrawlHolidays: Record<number, string> = Object.fromEntries(
    flatten(
        Object.entries({
            arathiBlizzard: [ 666, 673, 680, 697, 737 ],
            classicAshran: [ 1120, 1121, 1122, 1123, 1124 ],
            compStomp: [ 1234, 1235, 1236, 1237, 1238 ],
            cookingImpossible: [ 1047, 1048, 1049, 1050, 1051 ],
            deepSix: [ 702, 704, 705, 706, 736 ],
            deepwindDunk: [ 1239, 1240, 1241, 1242, 1243 ],
            gravityLapse: [ 659, 663, 670, 677, 684 ],
            packedHouse: [ 667, 674, 681, 688, 701 ],
            shadoPanShowdown: [ 1232, 1233, 1244, 1245, 1246, 1312 ],
            southshoreVsTarrenMill: [ 660, 662, 669, 676, 683 ],
            templeOfHotmogu: [ 1166, 1167, 1168, 1169, 1170 ],
            warsongScramble: [ 664, 671, 678, 685, 1221 ],
        })
        .map(([key, values]) => values.map((id) => [id, key]))
    )
)


function couldGet(slug: string, char: Character): boolean {
    const staticData = get(staticStore)

    const profession = staticData.professions[professionSlugToId[slug]]
    return !!char.professions?.[profession.id]?.[profession.subProfessions[9].id]
}

function getLatestSkill(char: Character, slug: string, minSkill: number): string {
    const staticData = get(staticStore)

    const professionId = professionSlugToId[slug]
    const subProfessions = staticData.professions[professionId].subProfessions
    const skill = char.professions[professionId][subProfessions[subProfessions.length - 1].id] ?.currentSkill ?? 0

    return skill < minSkill ? `Need ${minSkill} skill` : ''
}
