//Definir Globalmente as variáveis:
static string licenseFilePath = @"C:\Program Files\seu-sistema\seu-arquivo-Licenca.ini";
static string folderId = ""; // sua ID da pasta onde os arquivos de Licença estão armazenados no Drive
 
static bool ValidateLocalDefaultLicense()
    {
        // Caminhos dos arquivos de Licença local
        string[] licenseFilePaths = {
    @"C:\Program Files\seu-sistema\seu-arquivo-Licenca.dll",
    @"C:\Windows\System\seu-arquivo-Licenca.dll",
};

        // Verifica se todos os arquivos de Licença existem em todos os diretórios
        foreach (var path in licenseFilePaths)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"Licença padrão não encontrada", "Validação de Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Leitura do primeiro arquivo de Licença encontrado (minha estrutura dentro do arquivo Local)
        string firstLicenseFile = licenseFilePaths.FirstOrDefault(File.Exists);
        string[] lines = File.ReadAllLines(firstLicenseFile);
        string LicEmpresa = lines.Length > 0 ? lines[0].Trim() : "";
        string LicCnpjEmpresa = lines.Length > 1 ? lines[1].Trim() : "";
        string LicDeveloper = lines.Length > 2 ? lines[2].Trim() : "";
        string LicSoftware = lines.Length > 3 ? lines[3].Trim() : "";
        string LicVersaoSoftware = lines.Length > 4 ? lines[4].Trim() : "";

        // Verificação dos dados da Licença (exemplo do que pode conter de parametros dentro de uma dll para impossibilitar a sua abertura normalmente)
        if (LicEmpresa != "informacao1=" &&
            LicCnpjEmpresa != "informacao2=" &&
            LicDeveloper != "informacao3=" &&
            LicSoftware != "informacao4=" &&
            LicVersaoSoftware != "informacao5=")
        {
            MessageBox.Show("Licença local inválida!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    static Tuple<string, string> GetEmpresaFromDatabase(string emprNome, string emprCnpj)
    {
        // Chama o método para obter as empresas do banco de dados
        List<Dictionary<string, string>> empresas = new DALEmpresa().GetEmpresasValidationLicense();

        // Procura a empresa no banco de dados
        foreach (var empresa in empresas)
        {
            if (empresa["EmprNome"].Trim() == emprNome.Trim() && empresa["EmprCnpj"].Trim() == emprCnpj.Trim())
            {
                return Tuple.Create(empresa["EmprNome"].Trim(), empresa["EmprCnpj"].Trim());
            }
        }

        return null;
    }

    static List<Dictionary<string, string>> GetEmpresasFromLicenseFile()
    {
        List<Dictionary<string, string>> empresas = new List<Dictionary<string, string>>();

        try
        {
            string[] lines = File.ReadAllLines(licenseFilePath);
            Dictionary<string, string> empresa = new Dictionary<string, string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[LICENSE]"))
                {
                    if (empresa.Count > 0)
                    {
                        empresas.Add(empresa);
                        empresa = new Dictionary<string, string>();
                    }
                }
                else
                {
                    string[] parts = lines[i].Split('=');
                    if (parts.Length == 2)
                    {
                        empresa[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            if (empresa.Count > 0)
            {
                empresas.Add(empresa);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro ao ler a Licença local: " + ex.Message, "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return empresas;
    }

    static bool ValidateOnlineLicense()
    {
        try
        {
            // Obter dados da primeira empresa do arquivo ConnectLicense.ini
            List<Dictionary<string, string>> empresas = GetEmpresasFromLicenseFile();

            if (empresas.Count > 0)
            {
                Dictionary<string, string> primeiraEmpresa = empresas[0];
                string emprNome = primeiraEmpresa["emprNome"].Trim();
                string emprCnpj = primeiraEmpresa.ContainsKey("emprCnpj") ? primeiraEmpresa["emprCnpj"].Trim() : null;

                // Verificar se o emprCnpj está vazio no arquivo ConnectLicense.ini
                if (string.IsNullOrEmpty(emprCnpj))
                {
                    MessageBox.Show("CNPJ da Licença não encontrado!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Verificar se o CNPJ da empresa existe no banco de dados
                if (GetEmpresaFromDatabase(emprNome, emprCnpj) == null)
                {
                    MessageBox.Show("CNPJ da Empresa, para validação da licença, não encontrado!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Verificar se o arquivo de Licença está presente no Google Drive
                string fileName = "Licenses.txt"; // Nome do arquivo no Drive
                if (!CheckLicenseFileOnDrive(fileName))
                {
                    MessageBox.Show("Licença on-line não encontrada!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!IsLicenseAuthorized(fileName, emprCnpj))
                {
                    MessageBox.Show("Licença do Software consta inativa ou homologada!\n" +
                        "Contate o Suporte para validação de uma nova Licença de uso, utilizando facilmente o QRCODE Whatsapp", "Licença de uso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    frmSupport formSupport = new frmSupport();
                    formSupport.ShowDialog(); // Isso faz com que o formulário fique em primeiro plano até ser fechado

                    return false;
                }

                //MessageBox.Show("Licença online validada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            else
            {
                MessageBox.Show("Nenhuma empresa encontrada para a Licença!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro ao validar a Licença on-line: " + ex.Message, "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    static bool IsInternetConnected()
    {
        try
        {
            // Verifica se o sistema está bloqueado
            if (IsSystemBlocked())
            {
                return true; // Sistema bloqueado, retorna true para desbloquear temporariamente
            }

            // Caso o sistema não esteja bloqueado, verifica a conexão com a internet
            using (var client = new WebClient())
            using (client.OpenRead("http://www.google.com"))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    static bool IsSystemBlocked()
    {
        try
        {
            string[] lines = File.ReadAllLines(licenseFilePath);
            bool foundAuth = false;
            bool authValue = false;

            foreach (var line in lines)
            {
                if (line.Trim() == "[Auth]")
                {
                    foundAuth = true;
                    authValue = true;
                    break; // Encontrou
                }
            }

            // Se encontrar [Auth] e o valor false, retorna true indicando que o sistema está desbloqueado
            if (foundAuth && authValue)
            {
                // Abre o formulário de login
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmLogin());

                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro ao ler a Licença local: " + ex.Message, "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false; // Se ocorreu algum erro, considera o sistema bloqueado
        }

        // Se não encontrar [Auth] ou [Auth] com outro valor, retorna false indicando que o sistema está bloqueado
        return false;
    }

    static bool CheckLicenseFileOnDrive(string fileName)
    {
        try
        {
            // Configuração das credenciais
            string[] scopes = { DriveService.Scope.DriveReadonly };
            UserCredential credential;

            // Carrega as credenciais do cliente
            using (var stream = new FileStream("clientupd.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true)).Result;
            }

            // Verifica se as credenciais foram carregadas com sucesso
            if (credential == null)
            {
                MessageBox.Show("Falha ao carregar as credenciais do cliente!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Cria o serviço do Google Drive
            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "id do projeto API"
            });

            // Define o critério de busca do arquivo
            var request = driveService.Files.List();
            request.Q = $"name='{fileName}' and '{folderId}' in parents";

            // Executa a busca e verifica se o arquivo foi encontrado
            var driveFiles = request.Execute().Files;
            return driveFiles != null && driveFiles.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro ao verificar a Licença on-line: " + ex.Message, "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    static bool IsLicenseAuthorized(string fileName, string emprCnpj)
    {
        try
        {
            // Configuração das credenciais
            string[] scopes = { DriveService.Scope.DriveReadonly };
            UserCredential credential;

            // Carrega as credenciais do cliente
            using (var streamCredential = new FileStream("clientupd.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(streamCredential).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true)).Result;
            }

            // Verifica se as credenciais foram carregadas com sucesso
            if (credential == null)
            {
                MessageBox.Show("Falha ao carregar as credenciais do cliente!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Cria o serviço do Google Drive
            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "seu nome projeto API"
            });

            // Define o critério de busca do arquivo
            var request = driveService.Files.List();
            request.Q = $"name='{fileName}' and '{folderId}' in parents";

            // Executa a busca e verifica se o arquivo foi encontrado
            var driveFiles = request.Execute().Files;
            if (driveFiles == null || driveFiles.Count == 0)
            {
                MessageBox.Show("Arquivo de Licença on-line não encontrado!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else if (driveFiles.Count > 1)
            {
                MessageBox.Show("Mais de um arquivo de Licença encontrado!", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Obtém o ID do arquivo
            var fileId = driveFiles[0].Id;

            // Faz o download do conteúdo do arquivo
            var requestFileContent = driveService.Files.Get(fileId);
            var streamFileContent = new MemoryStream();
            requestFileContent.Download(streamFileContent);

            // Converte o conteúdo do arquivo em uma string
            streamFileContent.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(streamFileContent))
            {
                string fileContent = reader.ReadToEnd();

                // Dividir o conteúdo do arquivo em seções
                string[] sections = fileContent.Split(new[] { "[LICENSEAUTHORIZATION]" }, StringSplitOptions.RemoveEmptyEntries);

                // Iterar sobre cada seção de autorização
                foreach (string section in sections)
                {
                    // Verificar se a seção contém o CNPJ e a autorização corretos
                    if (section.Contains($"cnpj={emprCnpj}") && section.Contains("authorization=1"))
                    {
                        // Encontrou uma licença válida para o CNPJ
                        //MessageBox.Show("Licença do Software está ativa.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                }

                // Se chegou aqui, a licença está inativa ou o CNPJ não foi encontrado
                //MessageBox.Show("Licença do Software consta inativa ou CNPJ não encontrado no arquivo da Licença.", "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro ao verificar a autorização da Licença no Google Drive: " + ex.Message, "Validação Licença", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
