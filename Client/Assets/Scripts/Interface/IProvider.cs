namespace Assets.Scripts.Interface
{
    /// <summary>
    /// TIn → TOut 단순 제공자 인터페이스.
    ///
    /// - 입력 키(TIn)에 대응하는 값을 "있는 그대로" 제공한다.
    /// - 내부 저장소에서 조회하는 역할에 가깝다.
    /// - 해석(interpretation)이나 변환 로직을 포함하지 않는다.
    /// - 상태 계산이나 규약 적용 없이, 보유한 데이터를 그대로 반환한다.
    ///
    /// 예:
    /// - EntryMap에서 staticKey로 ContentEntry를 조회
    /// - 캐시/레지스트리에서 객체를 반환
    ///
    /// 해석이 필요한 경우는 IInterpreter 또는 IResolver가 담당한다.
    /// </summary>
    public interface IProvider<Tin, Tout>
    {
        bool TryGet(Tin input, out Tout output);
    }
}
