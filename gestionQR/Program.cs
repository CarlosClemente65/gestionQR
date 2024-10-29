using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Xobject;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Colors;
using QRCoder;
using ZXing.QrCode.Internal;

internal class Program
{
    private static void Main(string[] args)
    {
        //Variables
        string ficheroPDF = string.Empty; //Fichero PDF con las imagenes a extraer
        string textoSalidaQR = string.Empty; //Texto que se ha extraido del codigo QR
        string ficheroSalidaQR = string.Empty; //Nombre del fichero de salida con el texto del QR
        string textoEntradaQR = string.Empty; //Texto que se pasa por parametro para generar el QR
        string opcion = string.Empty; //Opcion para generar la imagen o extraer el texto

        //Si existe el fichero 'errores.txt' lo elimina
        if (File.Exists("errores.txt"))
        {
            File.Delete("errores.txt");
        }

        //Control de argumentos
        if (args.Length > 0 || args.Length < 3)
        {
            switch (args[0])
            {
                //Prepara los ficheros para procesar el PDF
                case "-p":
                    try
                    {
                        ficheroPDF = args[1];
                        ficheroSalidaQR = ficheroPDF.Substring(0, ficheroPDF.LastIndexOf('.')) + ".txt";
                        opcion = "decodificar";
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText("errores.txt", $"Error al leer el fichero {args[1]}\nInformacion adicional: " + ex.Message);
                    }
                    break;

                //Lee el fichero de entrada con el texto para generar el QR
                case "-f":
                    try
                    {
                        StreamReader sr = new StreamReader(args[1]);
                        textoEntradaQR = sr.ReadToEnd();
                        opcion = "codificar";
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText("errores.txt", $"Error al leer el fichero {args[1]}\nInformacion adicional: " + ex.Message);
                    }
                    break;

                //Mensaje de ayuda
                case "-h":
                    mostrarAyuda("");
                    return;

                //Almacena el texto pasado como argumento para generar el QR
                case "-t":
                    textoEntradaQR = args[1];
                    opcion = "codificar";
                    break;

                default:
                    mostrarAyuda("");
                    return;
            }
        }
        else
        {
            mostrarAyuda("Parametros incorrectos");
            return;
        }

        switch (opcion)
        {
            case "decodificar":
                try
                {
                    //Carga el documento PDF
                    PdfDocument pdfDocument = new PdfDocument(new PdfReader(ficheroPDF));
                    //Contador de imagenes para la salida
                    int idImagen = 1;

                    //Procesado de todas las paginas del PDF
                    for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
                    {
                        //Carga los recursos de la pagina
                        PdfPage page = pdfDocument.GetPage(pageNumber);
                        PdfResources resources = page.GetResources();

                        //Procesa todos los recursos cargados
                        foreach (PdfName xObjectKey in resources.GetResourceNames(PdfName.XObject))
                        {
                            //Carga la imagen para procesarla
                            PdfStream xObjectStream = resources.GetResource(PdfName.XObject).GetAsStream(xObjectKey);

                            if (xObjectStream != null)
                            {
                                //PdfDictionary xObjectDict = resources.GetResource(PdfName.XObject).GetAsDictionary(xObjectKey);
                                //if (xObjectDict != null && xObjectDict.Get(PdfName.Output) != null)
                                //{

                                try
                                {
                                    //if (xObjectDict.GetAsName(PdfName.Subtype) == PdfName.Image)
                                    //{
                                    PdfImageXObject imageXObject = new PdfImageXObject(xObjectStream);
                                    float ancho = imageXObject.GetWidth();
                                    float alto = imageXObject.GetHeight();

                                    byte[] imageBytes = null;
                                    if (ancho > 0 || alto > 0)
                                    {
                                        imageBytes = imageXObject.GetImageBytes();

                                    }

                                    if (imageBytes != null)
                                    {
                                        using (MemoryStream stream = new MemoryStream(imageBytes))
                                        {
                                            //Convierte la imagen a bitmap
                                            Bitmap imagenBitmap = new Bitmap(stream);

                                            //Obtiene el texto del QR
                                            string textoQR = DecodificarQR(imagenBitmap);

                                            //Almacena el texto del QR para luego guardarlo en el fichero
                                            textoSalidaQR += $"TextoQR {idImagen}: {textoQR} \n";
                                        }
                                    }
                                    //}
                                }

                                catch (Exception ex)
                                {
                                    File.WriteAllText(ficheroPDF, $"Error al procesar el PDF. {ex}");
                                }
                                //}
                                //else
                                //{
                                //    File.WriteAllText(ficheroPDF, "Error al procesar el PDF.");
                                //}
                            }
                            else
                            {
                                File.WriteAllText(ficheroPDF, "Error al procesar el PDF.");
                            }

                            //Incrementa el contador de imagenes
                            idImagen++;

                        }
                    }

                    //Al finalizar el procesado del PDF se guarda el texto en un fichero
                    File.WriteAllText(ficheroSalidaQR, textoSalidaQR);
                    pdfDocument.Close();
                }

                catch (Exception ex)
                {
                    File.WriteAllText("errores.txt", $"Error al procesar el fichero {ficheroPDF}\nInformacion adicional: " + ex.Message);
                }
                break;

            case "codificar":
                try
                {
                    using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(textoEntradaQR, QRCodeGenerator.ECCLevel.Q, true))
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(3, false);
                        File.WriteAllBytes("imagen2.png", qrCodeImage);
                    }
                }

                catch (Exception ex)
                {
                    File.WriteAllText("errores.txt", $"Error al generar el fichero con la imagen QR\nInformacion adicional: " + ex.Message);
                }
                break;
        }
    }


    private static void mostrarAyuda(string mensaje)
    {
        Console.WriteLine(mensaje);
        Console.WriteLine("\nUso de la aplicacion: leerQR [-p fichero.pdf | -t texto | -f fichero.txt -h]");
        Console.WriteLine("\nParametros");
        Console.WriteLine("  -p\tfichero PDF que contiene las imagenes");
        Console.WriteLine("  -f\tfichero .txt que contiene el texto a insertar en el QR");
        Console.WriteLine("  -t\ttexto para generar QR (entre comillas)");
        Console.WriteLine("  -h\tEsta ayuda");
    }

    public static string DecodificarQR(Bitmap imagenQR)
    {
        string textoQR = string.Empty;

        // Instancia un lector para leer el QR
        var reader = new ZXing.BarcodeReader();
        var result = reader.Decode(imagenQR);
        if (result != null)
        {
            textoQR = result.Text;
        }

        return textoQR;
    }
}