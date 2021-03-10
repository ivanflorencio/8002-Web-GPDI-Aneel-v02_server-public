using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PeD.Core.Models;
using TaesaCore.Extensions;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services
{
    public class ArquivoService : BaseService<FileUpload>
    {
        protected IConfiguration Configuration;
        protected readonly string StoragePath;

        public ArquivoService(IRepository<FileUpload> repository, IConfiguration configuration) : base(repository)
        {
            Configuration = configuration;
            StoragePath = Configuration.GetValue<string>("StoragePath");
        }

        protected string NewFileName()
        {
            return NewFileName(Path.GetRandomFileName());
        }

        protected string NewFileName(string filename)
        {
            if (string.IsNullOrEmpty(StoragePath))
                throw new ApplicationException("Caminho para upload não foi configurado");
            if (string.IsNullOrWhiteSpace(filename))
                throw new ApplicationException("Nome de arquivo inválido");

            return Path.Combine(GetFolderByDate(), filename.ToFileName());
        }

        private string GetFolderByDate()
        {
            return GetFolderByDate(DateTime.Today);
        }

        private string GetFolderByDate(DateTime dateTime)
        {
            if (string.IsNullOrEmpty(StoragePath))
                throw new ApplicationException("Caminho para upload não foi configurado");
            var folder = Path.Combine(StoragePath, dateTime.Year.ToString(), dateTime.Month.ToString(),
                dateTime.Day.ToString());
            Directory.CreateDirectory(folder);
            return folder;
        }

        public async Task<List<FileUpload>> SaveFiles(List<IFormFile> files)
        {
            var arquivos = new List<FileUpload>();
            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                var arquivo = new FileUpload()
                {
                    ContentType = file.ContentType
                };


                if (string.IsNullOrEmpty(StoragePath))
                    throw new ApplicationException("Caminho para upload não foi configurado");

                var fullFileName = NewFileName();
                using (var stream = File.Create(fullFileName))
                {
                    await file.CopyToAsync(stream);
                    stream.Close();
                    arquivo.Path = fullFileName;
                    Post(arquivo);
                    arquivos.Add(arquivo);
                }
            }

            return arquivos;
        }

        public async Task<FileUpload> SaveFile(IFormFile file)
        {
            var files = await SaveFiles(new List<IFormFile>() {file});
            return files.FirstOrDefault();
        }

        public FileUpload FromPath(string path, string mimetype, string filename = null)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Arquivo não encontrado", path);

            var fullFileName = filename != null ? NewFileName(filename) : NewFileName();

            File.Copy(path, fullFileName, true);

            var arquivo = new FileUpload()
            {
                ContentType = mimetype,
                Path = fullFileName
            };


            Post(arquivo);
            return arquivo;
        }
    }
}