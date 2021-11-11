import { CharacterClass } from '@/types'
import { ArmorType, PrimaryStat, WeaponType } from '@/types/enums'


export const classMap: Record<number, CharacterClass> = {
    1: new CharacterClass(
        1,
        'Warrior',
        'class_warrior',
        [71, 72, 73],
        [
            PrimaryStat.Strength,
        ],
        ArmorType.Plate,
        [
            WeaponType.OneHandedAxe,
            WeaponType.TwoHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.TwoHandedMace,
            WeaponType.OneHandedSword,
            WeaponType.TwoHandedSword,
            WeaponType.Dagger,
            WeaponType.Fist,
            WeaponType.Polearm,
            WeaponType.Stave,
            //WeaponType.Bow,
            //WeaponType.Crossbow,
            //WeaponType.Gun,
            WeaponType.Thrown,
            WeaponType.Shield,
        ],
    ),
    2: new CharacterClass(
        2,
        'Paladin',
        'class_paladin',
        [65, 66, 70],
        [
            PrimaryStat.Intellect,
            PrimaryStat.Strength,
        ],
        ArmorType.Plate,
        [
            WeaponType.OneHandedAxe,
            WeaponType.TwoHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.TwoHandedMace,
            WeaponType.OneHandedSword,
            WeaponType.TwoHandedSword,
            WeaponType.OffHand,
            WeaponType.Polearm,
            WeaponType.Shield,
        ],
    ),
    3: new CharacterClass(
        3,
        'Hunter',
        'class_hunter',
        [253, 254, 255],
        [
            PrimaryStat.Agility,
        ],
        ArmorType.Mail,
        [
            WeaponType.OneHandedAxe,
            WeaponType.TwoHandedAxe,
            WeaponType.OneHandedSword,
            WeaponType.TwoHandedSword,
            WeaponType.Dagger,
            WeaponType.Fist,
            WeaponType.Polearm,
            WeaponType.Bow,
            WeaponType.Crossbow,
            WeaponType.Gun,
        ],
    ),
    4: new CharacterClass(
        4,
        'Rogue',
        'class_rogue',
        [259, 260, 261],
        [
            PrimaryStat.Agility,
        ],
        ArmorType.Leather,
        [
            WeaponType.OneHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.OneHandedSword,
            WeaponType.Dagger,
            WeaponType.Fist,
            //WeaponType.Bow,
            //WeaponType.Crossbow,
            //WeaponType.Gun,
            WeaponType.Thrown,
        ],
    ),
    5: new CharacterClass(
        5,
        'Priest',
        'class_priest',
        [256, 257, 258],
        [
            PrimaryStat.Intellect,
        ],
        ArmorType.Cloth,
        [
            WeaponType.OneHandedMace,
            WeaponType.Dagger,
            WeaponType.OffHand,
            WeaponType.Stave,
            WeaponType.Wand,
        ],
    ),
    6: new CharacterClass(
        6,
        'Death Knight',
        'class_death_knight',
        [250, 251, 252],
        [
            PrimaryStat.Strength,
        ],
        ArmorType.Plate,
        [
            WeaponType.OneHandedAxe,
            WeaponType.TwoHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.TwoHandedMace,
            WeaponType.OneHandedSword,
            WeaponType.TwoHandedSword,
            WeaponType.Polearm,
        ],
    ),
    7: new CharacterClass(
        7,
        'Shaman',
        'class_shaman',
        [262, 263, 264],
        [
            PrimaryStat.Agility,
            PrimaryStat.Intellect,
        ],
        ArmorType.Mail,
        [
            WeaponType.OneHandedAxe,
            WeaponType.TwoHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.TwoHandedMace,
            WeaponType.Dagger,
            WeaponType.Fist,
            WeaponType.OffHand,
            WeaponType.Stave,
            WeaponType.Shield,
        ],
    ),
    8: new CharacterClass(
        8,
        'Mage',
        'class_mage',
        [62, 63, 64],
        [
            PrimaryStat.Intellect,
        ],
        ArmorType.Cloth,
        [
            WeaponType.OneHandedSword,
            WeaponType.Dagger,
            WeaponType.OffHand,
            WeaponType.Stave,
            WeaponType.Wand,
        ],
    ),
    9: new CharacterClass(
        9,
        'Warlock',
        'class_warlock',
        [265, 266, 267],
        [
            PrimaryStat.Intellect,
        ],
        ArmorType.Cloth,
        [
            WeaponType.OneHandedSword,
            WeaponType.Dagger,
            WeaponType.OffHand,
            WeaponType.Stave,
            WeaponType.Wand,
        ],
    ),
    10: new CharacterClass(
        10,
        'Monk',
        'class_monk',
        [268, 269, 270],
        [
            PrimaryStat.Agility,
            PrimaryStat.Intellect,
        ],
        ArmorType.Leather,
        [
            WeaponType.OneHandedAxe,
            WeaponType.OneHandedMace,
            WeaponType.OneHandedSword,
            WeaponType.Fist,
            WeaponType.OffHand,
            WeaponType.Polearm,
            WeaponType.Stave,
        ],
    ),
    11: new CharacterClass(
        11,
        'Druid',
        'class_druid',
        [102, 103, 104, 105],
        [
            PrimaryStat.Agility,
            PrimaryStat.Intellect,
        ],
        ArmorType.Leather,
        [
            WeaponType.OneHandedMace,
            WeaponType.Dagger,
            WeaponType.Fist,
            WeaponType.OffHand,
            WeaponType.Polearm,
            WeaponType.Stave,
        ],
    ),
    12: new CharacterClass(
        12,
        'Demon Hunter',
        'class_demon_hunter',
        [577, 581],
        [
            PrimaryStat.Agility,
        ],
        ArmorType.Leather,
        [
            WeaponType.Fist,
            WeaponType.OneHandedAxe,
            WeaponType.OneHandedSword,
            WeaponType.Warglaive,
        ],
    ),
}

export const classNameMap: Record<string, CharacterClass> = Object.fromEntries(
    Object.entries(classMap)
        .map(([, cls]) => [cls.name, cls]),
)

export const classSlugMap: Record<string, CharacterClass> = Object.fromEntries(
    Object.entries(classMap)
        .map(([, cls]) => [
            cls.name.toLowerCase().replace(' ', '-'),
            cls,
        ]),
)

export const classIdToSlug: Record<number, string> = Object.fromEntries(
    Object.entries(classSlugMap)
        .map(([slug, cls]) => [cls.id, slug])
)
