// Domain.cs
// -----------------------------------------------------------------------------
// DomainKey 32bit 레이아웃에서 사용하는 도메인 / 롤 / 등급 정의 및 규약 문서.
//
// 32bit DomainKey 구성 (상위 → 하위):
//
//  [ 31 … 28 ]   [ 27 … 24 ]   [ 23 … 20 ]  [ 19 … 12 ]    [ 11 … 0 ]
//    Domain         Grade         Role         Class        Instance
//    4bit           4bit          4bit         8bit         12bit
//
// - Domain  : 필드 위에서 최상위 분류 (아이템, 몬스터, NPC, 투사체, 월드오브젝트 등)
// - Grade   : 등급/티어/레어리티 (스컬 티어, 일반/엘리트/보스 등)
// - Role    : Domain 내부의 역할/타입 (근접/원거리, 무기/방어구, 파괴 가능 오브젝트 등)
// - Class   : 동일 Role/Grade 안에서의 세부 클래스(개별 종류)
// - Instance: 런타임 인스턴스 식별 번호 (0~4095), 세이브에는 사용하지 않음.
//
// 헥사 표현 예시: 0xA B C D E F G H
//   A   : Domain (4bit)
//   B   : Grade  (4bit)
//   C   : Role   (4bit)
//   D E : Class  (8bit)
//   F G H : Instance (하위 12bit, 런타임 인스턴스 식별)
//
// 정적 ID(StaticId)는 Instance를 제외한 상위 20bit를 의미하며,
// 테이블/세이브/리소스 매핑에서 사용된다.
// -----------------------------------------------------------------------------

namespace Assets.Scripts.Data
{
    /// <summary>
    /// DomainKey 의 최상위 4bit 에 사용되는 도메인 정의.
    /// 
    /// - 값 범위: 0x0 ~ 0xF (4bit)
    /// - 실제 사용 중인 도메인만 정의하고, 나머지는 예약(reserved)으로 남겨둔다.
    /// </summary>
    public enum Domain : byte
    {
        None = 0x0,

        /// <summary>
        /// 인벤토리/필드 드랍/장비 등 모든 아이템 계열.
        /// 예: 무기, 방어구, 소비, 재화, 스컬(머리) 등.
        /// </summary>
        Item = 0x1,

        /// <summary>
        /// 플레이어 캐릭터 및 플레이어가 조종하는 유닛.
        /// </summary>
        Character = 0x2,

        /// <summary>
        /// 적 몬스터 계열.
        /// </summary>
        Monster = 0x3,

        /// <summary>
        /// 투사체, 탄막, 발사체 계열.
        /// 예: 화살, 마법탄, 보스 패턴 발사체 등.
        /// </summary>
        Projectile = 0x4,

        /// <summary>
        /// 필드 위의 월드 오브젝트.
        /// 예: 파괴 가능한 오브젝트, 상호작용 기믹, 발판, 장치 등.
        /// </summary>
        WorldObject = 0x5,

        /// <summary>
        /// 대화 가능한 NPC, 상인, 퀘스트 제공자 등.
        /// </summary>
        Npc = 0x6,

        /// <summary>
        /// 씬/스테이지 프리팹 등 "맵 구성" 계열을 식별하는 도메인.
        /// </summary>
        Scene = 0x7, // 씬/레벨/맵 도메인

        // 0x8 ~ 0xF : 예약 (추가 도메인 확장용)
    }

    // -------------------------------------------------------------------------
    // Grade (4bit)
    // -------------------------------------------------------------------------
    // Grade 는 등급/티어/레어리티 표현용 공통 레벨.
    //
    // - 값 범위: 0x0 ~ 0xF (4bit)
    // - 스컬 티어, 아이템 레어리티, 몬스터 등급 등에 공통으로 사용 가능.
    // - Scene: DLC/AssetPack Index (Scene Pack 번호)
    // - 구체적인 의미는 Domain/Role 에 따라 다를 수 있음.
    // -------------------------------------------------------------------------
    public enum Grade : byte
    {
        None = 0x0,     // 등급 없음/기본

        // 스컬/아이템 티어 예시
        Grade1 = 0x1,    // common
        Grade2 = 0x2,    // uncommon
        Grade3 = 0x3,    // rare
        Grade4 = 0x4,    // epic
        Grade5 = 0x5,    // legendary

        // 0x6 ~ 0xF : 확장 여유분
    }

    // -------------------------------------------------------------------------
    // Role (4bit)
    // -------------------------------------------------------------------------
    // Role 은 Domain 별로 따로 정의한다.
    // - MonsterRole, ItemRole, WorldObjectRole, NpcRole 등
    // - 값 범위: 0x0 ~ 0xF (4bit)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Item 도메인 내부 역할(타입) 정의.
    /// Domain.Item 과 함께 사용된다.
    /// </summary>
    public enum ItemRole : byte
    {
        None = 0x0,
        Weapon = 0x1,
        Armor = 0x2,
        Consumable = 0x3,
        Currency = 0x4,
        Skul = 0x5,
        // 0x6 ~ 0xF : 확장
    }

    public enum MonsterRole : byte
    {
        None = 0x0,

        Melee = 0x1,
        Ranged = 0x2,
        Summoner = 0x3,

        Elite = 0x4,
        Named = 0x5,
        Boss = 0x6,
        // 0x7 ~ 0xF : 확장
    }

    public enum WorldObjectRole : byte
    {
        None = 0x0,
        Destructible = 0x1,
        Interactable = 0x2,
        Platform = 0x3,
        Decoration = 0x4,
        // 0x5 ~ 0xF : 확장
    }

    public enum NpcRole : byte
    {
        None = 0x0,
        Talker = 0x1,
        Merchant = 0x2,
        QuestGiver = 0x3,
        // 0x4 ~ 0xF : 확장
    }

    // SceneRole
    public enum SceneRole : byte
    {
        None = 0x0,

        UnityScene = 0x1,
        StageData = 0x2,
        StagePrefab = 0x3,
        // 0x4 ~ 0xF : 확장
    }

    // StageRole(논리 스테이지 타입)
    public enum StageRole : byte
    {
        None = 0x0,

        Normal = 0x1,
        Hidden = 0x2,
        Shop = 0x3,
        MidBoss = 0x4,
        BossEntry = 0x5,
        Boss = 0x6,
    }

    // Class (8bit)에 대한 규약은 별도 파일/주석에서 관리
}
