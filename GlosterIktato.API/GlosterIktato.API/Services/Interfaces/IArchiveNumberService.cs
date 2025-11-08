namespace GlosterIktato.API.Services.Interfaces
{
    public interface IArchiveNumberService
    {
        Task<string> GenerateArchiveNumberAsync(int companyId, int documentTypeId);
    }
}
