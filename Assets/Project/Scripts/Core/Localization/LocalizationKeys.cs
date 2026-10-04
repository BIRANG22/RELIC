public static class LocalizationKeys
{
    public static class Common
    {
        public const string None = "common.none";
        public const string Confirm = "common.confirm";
        public const string Cancel = "common.cancel";
        public const string Close = "common.close";
        public const string Untranslated = "common.untranslated";
    }

    public static class Warning
    {
        public const string DataUnavailable = "warning.data.unavailable";
        public const string CharacterNotSelected = "warning.character.not_selected";
        public const string CharacterDataNotFound = "warning.character.data_not_found";
        public const string CharacterDataNotFoundWithId = "warning.character.data_not_found_with_id";
        public const string SelectCharacterFirst = "warning.character.select_first";
        public const string NoAvailableCharacter = "warning.character.none_available";
        public const string MaxLevel = "warning.character.max_level";
        public const string SkillSlotNotConnected = "warning.skill.slot_not_connected";
        public const string SelectSkillSlotFirst = "warning.skill.select_slot_first";
        public const string NoSkillSelected = "warning.skill.not_selected";
        public const string SkillMemoryUnlockLevel = "warning.skill.memory_unlock_level";
        public const string SkillLockedLevel = "warning.skill.locked_level";
        public const string SkillNotAvailableForSlot = "warning.skill.not_available_for_slot";
        public const string NoRuneSelected = "warning.rune.not_selected";
        public const string RuneNotAvailable = "warning.rune.not_available";
        public const string NoEmptyRuneSlot = "warning.rune.no_empty_slot";
        public const string SharedRuneLevelRequired = "warning.rune.shared_level_required";
        public const string CharacterRuneUnlockLevel = "warning.rune.character_unlock_level";
        public const string NoRuneToUnequip = "warning.rune.none_to_unequip";
        public const string RuneNotEquipped = "warning.rune.not_equipped";
        public const string RuneSlotNotConnected = "warning.rune.slot_not_connected";
        public const string RuneSlotLocked = "warning.rune.slot_locked";
        public const string RuneSlotUnlockLevel = "warning.rune.slot_unlock_level";
        public const string SharedRuneLocked = "warning.rune.shared_locked";
        public const string RuneEquippedByOtherCharacter = "warning.rune.equipped_by_other_character";
        public const string InsufficientBlueDustium = "warning.currency.insufficient_blue_dustium";
        public const string StageLocked = "warning.stage.locked";
        public const string QuestMustComplete = "warning.quest.must_complete";
        public const string PartySelectCharacter = "warning.party.select_character";
        public const string PartySelectDeployedCharacter = "warning.party.select_deployed_character";
        public const string MapNotSelected = "warning.map.not_selected";
        public const string PartyEmpty = "warning.party.empty";
        public const string PartyNotFull = "warning.party.not_full";
        public const string GameManagerMissing = "warning.system.game_manager_missing";
        public const string ElricDialogueRequired = "warning.tutorial.elric_dialogue_required";
        public const string NetworkHostOnlyStart = "warning.network.host_only_start";
        public const string NetworkBattleStartSyncFailed = "warning.network.battle_start_sync_failed";
        public const string NoRegisteredSkill = "warning.skill.none_registered";
        public const string SkillDataNotFound = "warning.skill.data_not_found";
        public const string TimelineControllerMissing = "warning.timeline.controller_missing";
        public const string RelicUnavailableNow = "warning.relic.unavailable_now";
        public const string TargetSelectionUnavailable = "warning.target_selection.unavailable";
        public const string SkillReservationMissing = "warning.skill.reservation_missing";
        public const string SlotOccupiedByOtherCharacter = "warning.timeline.slot_occupied_by_other_character";
        public const string SlotActionLimit = "warning.timeline.slot_action_limit";
        public const string SkillRangeMissing = "warning.skill.range_missing";
        public const string SelectableGridRangeMissing = "warning.grid.selectable_range_missing";
        public const string InsufficientMoveCost = "warning.move.insufficient_cost";
        public const string NoSelectableGrid = "warning.grid.none_selectable";
        public const string GridNotSelectable = "warning.grid.not_selectable";
        public const string MoveDestinationOccupied = "warning.move.destination_occupied";
        public const string MoveDestinationInvalid = "warning.move.destination_invalid";
        public const string MoveReservationFailed = "warning.move.reservation_failed";
        public const string SelectTimelineSlotFirst = "warning.timeline.select_slot_first";
        public const string BattleGridMissing = "warning.grid.battle_grid_missing";
        public const string TimelineSlotMissing = "warning.timeline.slot_missing";
        public const string TimelineSlotUnavailable = "warning.timeline.slot_unavailable";
        public const string CharacterPositionMissing = "warning.character.position_missing";
        public const string SkillReservationControllerMissing = "warning.skill.reservation_controller_missing";
        public const string SkillReservationFailed = "warning.skill.reservation_failed";
        public const string NoReservationToUndo = "warning.timeline.no_reservation_to_undo";
        public const string TimelineUiMissing = "warning.timeline.ui_missing";
        public const string ActionPreparationIncomplete = "warning.battle.action_preparation_incomplete";
        public const string RelicDataNotFound = "warning.relic.data_not_found";
        public const string EquipPanelUnavailable = "warning.equip.panel_unavailable";
        public const string FeatureUnavailable = "warning.feature.unavailable";
    }

    public static class Lobby
    {
        public const string PlayReady = "ui.lobby.play.ready";
        public const string PlayDepart = "ui.lobby.play.depart";
        public const string PanelErosion = "ui.lobby.panel.erosion";
        public const string PanelResonance = "ui.lobby.panel.resonance";
        public const string PanelCrafting = "ui.lobby.panel.crafting";
        public const string PanelStorage = "ui.lobby.panel.storage";
        public const string PanelMercenary = "ui.lobby.panel.mercenary";
        public const string CultureRecipe = "ui.lobby.culture.recipe";
        public const string CultureMaterial = "ui.lobby.culture.material";
    }

    public static class Dialog
    {
        public const string Yes = "common.yes";
        public const string No = "common.no";
        public const string QuitConfirm = "common.confirm_quit_game";
        public const string AbandonConfirm = "dialog.expedition.abandon_confirm";
        public const string PurchaseConfirm = "dialog.purchase.confirm";
        public const string RelicPurchaseConfirm = "dialog.relic.purchase_confirm";
        public const string RuneActivateConfirm = "dialog.rune.activate_confirm";
    }

    public static class Battle
    {
        public const string Start = "ui.battle.start";
        public const string ActionReserve = "ui.battle.action_reserve";
        public const string Progress = "ui.battle.progress";
        public const string SkillChangeLocked = "warning.battle.skill_change_locked";
        public const string RelicChangeLocked = "warning.battle.relic_change_locked";
        public const string TimelineSelectionLocked = "warning.battle.timeline_selection_locked";
    }

    public static class SystemMessage
    {
        public const string SaveSuccess = "system.save.success";
        public const string SaveFailed = "system.save.failed";
        public const string ExpeditionMissing = "system.expedition.missing";
        public const string ExpeditionAbandoned = "system.expedition.abandoned";
    }

    public static class Character
    {
        public const string Locked = "ui.character.locked";
    }

    public static class Skill
    {
        public const string Cost = "ui.skill.cost";
        public const string Type = "ui.skill.type";
        public const string Effect = "ui.skill.effect";
    }

    public static class Tutorial
    {
        public const string SpeakerElric = "tutorial.speaker.elric";
        public const string Intro01 = "tutorial.intro.01";
        public const string Intro02 = "tutorial.intro.02";
        public const string Intro03 = "tutorial.intro.03";
        public const string Intro04 = "tutorial.intro.04";
        public const string Intro05 = "tutorial.intro.05";
        public const string Intro06 = "tutorial.intro.06";
        public const string Intro07 = "tutorial.intro.07";
        public const string FirstExpedition01 = "tutorial.first_expedition.01";
        public const string FirstExpedition02 = "tutorial.first_expedition.02";
        public const string FirstExpedition03 = "tutorial.first_expedition.03";
        public const string SkipButton = "ui.tutorial.skip";
        public const string SkipConfirm = "dialog.tutorial.skip_confirm";
    }

    public static class IntroStory
    {
        public const string Line01 = "intro.story.01";
        public const string Line02 = "intro.story.02";
        public const string Line03 = "intro.story.03";
        public const string Line04 = "intro.story.04";
        public const string Line05 = "intro.story.05";
        public const string Line06 = "intro.story.06";
        public const string Line07 = "intro.story.07";
        public const string Line08 = "intro.story.08";
        public const string Line09 = "intro.story.09";
    }

    public static class Rune
    {
        public const string InfoTitle = "ui.rune.info.title";
        public const string InfoEmptyDescription = "ui.rune.info.empty_description";
        public const string NoEffectDescription = "common.no_effect_description";
    }

    public static class CharacterSetting
    {
        public const string PreviewInfo = "ui.character_setting.preview_info";
        public const string SkillInfoTitle = "ui.character_setting.skill_info_title";
        public const string SkillInfoEmpty = "ui.character_setting.skill_info_empty";
        public const string RuneInfoTitle = "ui.character_setting.rune_info_title";
        public const string RuneInfoEmpty = "ui.character_setting.rune_info_empty";
        public const string CharacterIntro = "lobby.character_intro";
        public const string KarmaAcquisitionTitle = "lobby.karma.acquisition_title";
        public const string KarmaDescription = "lobby.karma.description";
        public const string StatMaximumValue = "lobby.stat.maximum_value";
    }

    public static class SkillInfo
    {
        public const string Cost = "ui.skill.cost";
        public const string Type = "ui.skill.type";
        public const string Effect = "ui.skill.effect";
        public const string NoCost = "common.no_cost";
        public const string NoEffect = "common.no_effect";
        public const string RarityMove = "ui.skill.rarity.move";
        public const string RarityMemory = "ui.skill.rarity.memory";
        public const string RarityCommonMemory = "ui.skill.rarity.common_memory";
        public const string RarityRareMemory = "ui.skill.rarity.rare_memory";
        public const string RarityEpicMemory = "ui.skill.rarity.epic_memory";
        public const string RarityUniqueMemory = "ui.skill.rarity.unique_memory";
        public const string RarityInstinctMemory = "ui.skill.rarity.instinct_memory";
        public const string RarityManifestationMemory = "ui.skill.rarity.manifestation_memory";
        public const string RarityImplementationMemory = "ui.skill.rarity.implementation_memory";
        public const string RangeDirection = "ui.skill.range.direction";
        public const string RangeSelection = "ui.skill.range.selection";
        public const string RangePassive = "ui.skill.range.passive";
        public const string PassiveActivation = "ui.skill.range.passive_activation";
    }

    public static class Record
    {
        public const string UseTarget = "ui.record.use_target";
        public const string UseCount = "ui.record.use_count";
        public const string UnlockCharacterLevel = "ui.record.unlock_character_level";
        public const string UnlockBlueDustium = "ui.record.unlock_blue_dustium";
        public const string Method = "ui.record.method";
        public const string Consumption = "ui.record.consumption";
        public const string Effect = "ui.record.effect";
        public const string UnknownMethod = "ui.record.unknown_method";
        public const string UnknownConsumption = "ui.record.unknown_consumption";
        public const string UnknownEffect = "ui.record.unknown_effect";
        public const string UnknownUseTarget = "ui.record.unknown_use_target";
        public const string UnknownUseCount = "ui.record.unknown_use_count";
        public const string NoConsumption = "ui.record.no_consumption";
        public const string UnknownDescription = "ui.record.unknown_description";
    }

    public static class Resource { public const string Hp = "common.hp"; public const string Mana = "common.mana"; public const string Karma = "resource.karma"; public const string Move = "common.move"; }
    public static class Target { public const string Self = "ui.record.target.self"; public const string Grid = "ui.record.target.grid"; }
    public static class Range { public const string Direction = "ui.record.range.direction"; public const string Selection = "ui.record.range.selection"; public const string Passive = "ui.record.range.passive"; }

    public static class MemoryRarity
    {
        public const string Exclusive = "ui.record.memory_rarity.exclusive";
        public const string Common = "ui.record.memory_rarity.common";
        public const string Rare = "ui.record.memory_rarity.rare";
        public const string Epic = "ui.record.memory_rarity.epic";
        public const string Unique = "ui.record.memory_rarity.unique";
    }

    public static class FragmentRarity
    {
        public const string Exclusive = "ui.record.fragment_rarity.exclusive";
        public const string Common = "ui.record.fragment_rarity.common";
        public const string Rare = "ui.record.fragment_rarity.rare";
        public const string Unique = "ui.record.fragment_rarity.unique";
    }

    public static class ItemRarity { public const string Common = "ui.record.item_rarity.common"; public const string Rare = "ui.record.item_rarity.rare"; public const string Epic = "ui.record.item_rarity.epic"; }
    public static class CompoundRarity { public const string Common = "ui.record.compound_rarity.common"; public const string Rare = "ui.record.compound_rarity.rare"; public const string Epic = "ui.record.compound_rarity.epic"; }
    public static class RelicRarity { public const string Common = "ui.record.relic_rarity.common"; public const string Rare = "ui.record.relic_rarity.rare"; public const string Epic = "ui.record.relic_rarity.epic"; public const string Unique = "ui.record.relic_rarity.unique"; }
}
