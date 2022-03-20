using MinimalAPI.Models;

namespace MinimalAPI.ViewModel
{
    public class FornecedorViewModel
    {
        public FornecedorViewModel(string? nome, string? documento)
        {
            Nome = nome;
            Documento = documento;
        }
        public string? Nome { get; set; }
        public string? Documento { get; set; }
        public Fornecedor ToFornecedorEntity() =>
            new(this.Nome, this.Documento);
    }    
}
