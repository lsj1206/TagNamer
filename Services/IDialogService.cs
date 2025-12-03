using System.Threading.Tasks;

namespace TagNamer.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message, string title = "확인");
}
