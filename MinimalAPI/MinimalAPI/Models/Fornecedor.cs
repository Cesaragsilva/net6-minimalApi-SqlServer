using MinimalAPI.ViewModel;

namespace MinimalAPI.Models
{
    public class Fornecedor
    {
        public Fornecedor()
        {

        }
        public Fornecedor(string? nome, string? documento)
        {
            Id = Guid.NewGuid();
            Ativo = true;
            Nome = nome;
            Documento = documento;
        }
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Documento { get; set; }
        public bool Ativo { get; set; }
        public void Atualizar(string? nome, string? documento) { 
            Nome = nome;
            Documento= documento;
        }
    }
}
