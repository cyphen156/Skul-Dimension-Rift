namespace Assets.Scripts.Interface
{
    /// <summary>
    /// TIn → TOut 해석 포함 제공자 인터페이스.
    ///
    /// - 입력 값(TIn)을 기반으로 최종 결과(TOut)를 도출한다.
    /// - 내부적으로 해석, 변환, 조합 등의 로직이 포함될 수 있다.
    /// - 필요 시 IProvider 또는 IInterpreter를 조합하여 구현될 수 있다.
    ///
    /// 예:
    /// - staticKey → ContentEntry 조회 후 → 주소 문자열 생성
    /// - ID → 메타 조회 후 → 로컬 경로 계산
    ///
    /// 단순 저장소 조회가 아니라,
    /// "의미 있는 결과를 계산하여 반환"하는 역할을 가진다.
    /// </summary>
    public interface IResolver<Tin, Tout>
    {
        bool TryResolve(Tin input, out Tout output);
    }
}
