// Domain.cs
// -----------------------------------------------------------------------------
// ObjectKey 32bit 레이아웃에서 사용하는 도메인 / 롤 / 등급 정의 및 규약 문서.
//
// 32bit ObjectKey 구성 (상위 → 하위):
//
//  [ 31 … 28 ]   [ 27 … 24 ]   [ 23 … 16 ]  [ 15 … 12 ]    [ 11 … 0 ]
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
    /// ObjectKey 의 최상위 4bit 에 사용되는 도메인 정의.
    /// 
    /// - 값 범위: 0x0 ~ 0xF (4bit)
    /// - 실제 사용 중인 도메인만 정의하고, 나머지는 예약(reserved)으로 남겨둔다.
    /// </summary>
    public enum ObjectDomain : byte
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

        // 0x7 ~ 0xF : 예약 (추가 도메인 확장용)
    }

    // -------------------------------------------------------------------------
    // Role (8bit)
    // -------------------------------------------------------------------------
    // Role 은 ObjectDomain 별로 따로 정의한다.
    // - MonsterRole, ItemRole, WorldObjectRole, NpcRole 등
    // - 값 범위: 0x00 ~ 0xFF
    // - 실제로는 의미 있는 구간을 나누어 사용 (예: 0x00~0x1F 일반, 0x20~ 보스 등)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Item 도메인 내부 역할(타입) 정의.
    /// ObjectDomain.Item 과 함께 사용된다.
    /// </summary>
    public enum ItemRole : byte
    {
        None = 0x00,

        /// <summary>장비형 무기 아이템.</summary>
        Weapon = 0x01,

        /// <summary>장비형 방어구 아이템.</summary>
        Armor = 0x02,

        /// <summary>포션/스크롤 등 소비형 아이템.</summary>
        Consumable = 0x03,

        /// <summary>골드, 소울, 재료 등 재화/재료 아이템.</summary>
        Currency = 0x04,

        /// <summary>스컬 고유 머리(캐릭터 교체용) 아이템.</summary>
        Skull = 0x10,

        // 0x80~ 등은 DLC/확장용 예약
    }

    /// <summary>
    /// Monster 도메인 내부 역할(타입) 정의.
    /// ObjectDomain.Monster 와 함께 사용된다.
    /// </summary>
    public enum MonsterRole : byte
    {
        None = 0x00,

        /// <summary>기본 근접형 몬스터.</summary>
        Melee = 0x01,

        /// <summary>기본 원거리형 몬스터.</summary>
        Ranged = 0x02,

        /// <summary>소환/지원형 몬스터.</summary>
        Summoner = 0x03,

        /// <summary>강화된 일반 몬스터(엘리트 등).</summary>
        Elite = 0x10,

        /// <summary>고유 이름을 가진 네임드 몬스터.</summary>
        Named = 0x20,

        /// <summary>보스 몬스터.</summary>
        Boss = 0x30,

        // 추가 패턴/종류 필요시 0x40 이상 확장
    }

    /// <summary>
    /// WorldObject 도메인 내부 역할(타입) 정의.
    /// ObjectDomain.WorldObject 와 함께 사용된다.
    /// </summary>
    public enum WorldObjectRole : byte
    {
        None = 0x00,

        /// <summary>파괴 가능한 오브젝트(항아리, 상자 등).</summary>
        Destructible = 0x01,

        /// <summary>레버, 버튼 등 상호작용 기믹.</summary>
        Interactable = 0x02,

        /// <summary>발판, 플랫폼 등 지형 요소.</summary>
        Platform = 0x03,

        /// <summary>단순 장식용.</summary>
        Decoration = 0x10,
    }

    /// <summary>
    /// Npc 도메인 내부 역할(타입) 정의.
    /// ObjectDomain.Npc 와 함께 사용된다.
    /// </summary>
    public enum NpcRole : byte
    {
        None = 0x00,

        /// <summary>스토리/대화용 일반 NPC.</summary>
        Talker = 0x01,

        /// <summary>상점/거래 NPC.</summary>
        Merchant = 0x02,

        /// <summary>퀘스트 제공/완료용 NPC.</summary>
        QuestGiver = 0x03,
    }

    // -------------------------------------------------------------------------
    // Grade (4bit)
    // -------------------------------------------------------------------------
    // Grade 는 등급/티어/레어리티 표현용 공통 레벨.
    //
    // - 값 범위: 0x0 ~ 0xF (4bit)
    // - 스컬 티어, 아이템 레어리티, 몬스터 등급 등에 공통으로 사용 가능.
    // -------------------------------------------------------------------------

    public enum Grade : byte
    {
        None = 0x0,     // 등급 없음/기본

        // 스컬/아이템 티어 예시
        Tier1 = 0x1,    // common
        Tier2 = 0x2,    // uncommon
        Tier3 = 0x3,    // rare
        Tier4 = 0x4,    // epic
        Tier5 = 0x5,    // legendary

        // 0x6 ~ 0xF : 확장 여유분
    }

    // -------------------------------------------------------------------------
    // Class (4bit)에 대한 규약 (enum 은 도메인별/Role별 개별 파일로 둘 수도 있음)
    // -------------------------------------------------------------------------
    //
    // Class 는 동일 Domain + Role + Grade 안에서의 "구체적인 종류" 구분용 4bit 이다.
    // 예:
    //  - Monster + Role=Melee + Grade=Elite:
    //      0x0 : 해골검사
    //      0x1 : 해골방패병
    //      0x2 : 좀비 전사
    //  - Item + Role=Weapon + Grade=Rare:
    //      0x0 : 레어소드
    //      0x1 : 레어보우
    //
    // 구현 패턴 권장:
    //  - MonsterClass, ItemClass 등 도메인별/Role별 enum 을 별도 파일에 정의
    //  - 혹은 data table 에서 Class 값을 숫자로만 관리하고, enum 은 에디터용으로만 사용
    //
    // ※ 여기서는 규약만 문서화하고, 실제 Class enum 정의는
    //    각 도메인/Role 에 맞는 파일에서 관리한다.
    // -------------------------------------------------------------------------
}
