public List<Dictionary<string, string>> GetEmpresasValidationLicense()
{
    List<Dictionary<string, string>> empresas = new List<Dictionary<string, string>>();

    try
    {
        using (SqlConnection connection = new SqlConnection(Conexao.Connection.connectionString))
        {
            connection.Open();

            string sql = "SELECT EmprCod, EmprNome, EmprCnpj FROM Empresas";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, string> empresa = new Dictionary<string, string>();
                        empresa["EmprCod"] = reader["EmprCod"].ToString();
                        empresa["EmprNome"] = reader["EmprNome"].ToString();
                        empresa["EmprCnpj"] = reader["EmprCnpj"].ToString();
                        empresas.Add(empresa);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Ocorreu uma exceção: " + ex.Message);
    }

    return empresas;
}
