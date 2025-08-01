using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace BoletoNet
{
    internal class ArquivoRemessaCNAB400 : AbstractArquivoRemessa, IArquivoRemessa
    {

        #region Construtores

        public ArquivoRemessaCNAB400()
        {
            this.TipoArquivo = TipoArquivo.CNAB400;
        }

        #endregion

        #region M�todos de inst�ncia
        /// <summary>
        /// M�todo que far� a verifica��o se a classe est� devidamente implementada para a gera��o da Remessa
        /// </summary>
        public override bool ValidarArquivoRemessa(string numeroConvenio, IBanco banco, Cedente cedente, Boletos boletos, int numeroArquivoRemessa, out string mensagem)
        {
            try
            {
                bool vRetorno = true;
                string vMsg = string.Empty;
                if (boletos != null && boletos.Count > 0)
                {
                    Boleto boleto = boletos[0];

                    string vMsgBol = string.Empty;
                    bool vRetBol = boleto.Banco.ValidarRemessa(this.TipoArquivo, numeroConvenio, banco, cedente, boletos, numeroArquivoRemessa, out vMsgBol);
                    if (!vRetBol && !String.IsNullOrEmpty(vMsgBol))
                    {
                        vMsg += vMsgBol;
                        vRetorno = vRetBol;
                    }
                }
                mensagem = vMsg;
                return vRetorno;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public override void GerarArquivoRemessa(string numeroConvenio, IBanco banco, Cedente cedente, Boletos boletos, Stream arquivo, int numeroArquivoRemessa)
        {
            try
            {
                int numeroRegistro = 2;
                string strline;
                decimal vltitulostotal = 0;                 //Uso apenas no registro TRAILER do banco Santander - jsoda em 09/05/2012 - Add no registro TRAILER do banco Banrisul - sidneiklein em 08/08/2013

                StreamWriter incluiLinha = new StreamWriter(arquivo, Encoding.GetEncoding("ISO-8859-1"));
                cedente.Carteira = boletos[0].Carteira;
                strline = banco.GerarHeaderRemessa(numeroConvenio, cedente, TipoArquivo.CNAB400, numeroArquivoRemessa);
                incluiLinha.WriteLine(strline);

                foreach (Boleto boleto in boletos)
                {
                    boleto.Banco = banco;
                    strline = boleto.Banco.GerarDetalheRemessa(boleto, numeroRegistro, TipoArquivo.CNAB400);
                    incluiLinha.WriteLine(strline);
                    vltitulostotal += boleto.ValorBoleto;   //Uso apenas no registro TRAILER do banco Santander - jsoda em 09/05/2012 - Add no registro TRAILER do banco Banrisul - sidneiklein em 08/08/2013
                    numeroRegistro++;

                    // Banco CrediSis - 97
                    if (banco.Codigo == 97)
                    {
                        if (boleto.ValorDesconto > 0 || boleto.PercMulta > 0 || boleto.ValorMulta > 0)
                        {
                            Banco_CrediSis _banco = new Banco_CrediSis();
                            strline = _banco.GerarRegistroDetalhe2(boleto, numeroRegistro);
                            incluiLinha.WriteLine(strline);
                            numeroRegistro++;
                        }
                    }

                    // 85 - CECRED
                    if (banco.Codigo == 85) {
                        if (boleto.PercMulta > 0 || boleto.ValorMulta > 0) {
                            Banco_Cecred _banco = new Banco_Cecred();
                            string linhaCECREDRegistroDetalhe5 = _banco.GerarRegistroDetalhe5(boleto, numeroRegistro, TipoArquivo.CNAB400);
                            incluiLinha.WriteLine(linhaCECREDRegistroDetalhe5);
                            numeroRegistro++;
                        }
                    }

                    // 341 - Banco Itau
                    if (banco.Codigo == 341)
                    {
                        if (boleto.PercMulta > 0 || boleto.ValorMulta > 0)
                        {
                            Banco_Itau _banco = new Banco_Itau();
                            strline = _banco.GerarRegistroDetalhe2(boleto, numeroRegistro);
                            incluiLinha.WriteLine(strline);
                            numeroRegistro++;
                        }
                    }
                    // Banco Bradesco - 237
                    if (banco.Codigo == 237)
                    {
                        if (boleto.OutrosDescontos > 0)
                        {
                            Banco_Bradesco _banco = new Banco_Bradesco();
                            strline = _banco.GerarRegistroDetalhe2(boleto, numeroRegistro);
                            incluiLinha.WriteLine(strline);
                            numeroRegistro++;
                        }
                    }

                    if ((boleto.Instrucoes != null && boleto.Instrucoes.Count > 0) || (boleto.Sacado.Instrucoes != null && boleto.Sacado.Instrucoes.Count > 0))
                    {
                        strline = boleto.Banco.GerarMensagemVariavelRemessa(boleto, ref numeroRegistro, TipoArquivo.CNAB400);
                        if (!string.IsNullOrEmpty(strline) && !string.IsNullOrWhiteSpace(strline))
                        { 
                            incluiLinha.WriteLine(strline);
                            //numeroRegistro++;
                        }
                    }
                }

                strline = banco.GerarTrailerRemessa(numeroRegistro, TipoArquivo.CNAB400, cedente, vltitulostotal);

                incluiLinha.WriteLine(strline);

                incluiLinha.Close();
                incluiLinha.Dispose(); // Incluido por Luiz Ponce 07/07/2012.
                incluiLinha = null; // Incluido por Luiz Ponce 07/07/2012.
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar arquivo remessa.", ex);
            }
        }

        #endregion

    }
}
