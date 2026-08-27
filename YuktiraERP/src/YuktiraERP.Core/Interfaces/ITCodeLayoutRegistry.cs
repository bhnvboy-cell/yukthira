using YuktiraERP.Core.Domain.Transaction;

namespace YuktiraERP.Core.Interfaces;

public interface ITCodeLayoutRegistry
{
    TCodeLayoutConfig? Get(string tcode);
    IReadOnlyList<TCodeLayoutConfig> GetAll();
    void Register(TCodeLayoutConfig config);
}
