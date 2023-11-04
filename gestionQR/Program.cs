using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Xobject;
using System.Drawing;
using ZXing;

internal class Program
{
    private static void Main(string[] args)
    {
        //Variables
        string ficheroPDF = string.Empty;
        string textoImagenesQR = string.Empty;
        string ficheroSalidaQR = string.Empty;

        //Si existe el fichero 'errores.txt' lo elimina
        if (File.Exists("errores.txt"))
        {
            File.Delete("errores.txt");
        }

        //Control de argumentos
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "-f":
                    ficheroPDF = args[1];
                    ficheroSalidaQR = ficheroPDF.Substring(0, ficheroPDF.LastIndexOf('.')) + ".txt";
                    break;
                case "-h":
                    mostrarAyuda("");
                    return;
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
                    PdfImageXObject imageXObject = new PdfImageXObject(xObjectStream);
                    byte[] imageBytes = imageXObject.GetImageBytes();

                    Bitmap imagenBitmap;
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        //Convierte la imagen a bitmap
                        imagenBitmap = new Bitmap(stream);

                        //Obtiene el texto del QR
                        string textoQR = DecodificarQR(imagenBitmap);

                        //Almacena el texto del QR para luego guardarlo en el fichero
                        textoImagenesQR += $"TextoQR {idImagen}: {textoQR} \n";

                    }

                    //Incrementa el contador de imagenes
                    idImagen++;

                }
            }

            //Al finalizar el procesado del PDF se guarda el texto en un fichero
            File.WriteAllText(ficheroSalidaQR, textoImagenesQR);
            pdfDocument.Close();

        }

        catch (Exception ex)
        {
            File.WriteAllText("errores.txt", $"Error al procesar el fichero {ficheroPDF}\nInformacion adicional: " + ex.Message);
        }
    }

    private static void mostrarAyuda(string mensaje)
    {
        Console.WriteLine(mensaje);
        Console.WriteLine("\nUso de la aplicacion: leerQR [-f fichero.pdf | -h]");
        Console.WriteLine("\nParametros");
        Console.WriteLine("  -f\tfichero.pdf");
        Console.WriteLine("  -h\tEsta ayuda");
    }

    public static string DecodificarQR(Bitmap imagenQR)
    {
        string textoQR = string.Empty;

        //Instancia un lector para leer el QR
        BarcodeReader br = new BarcodeReader();

        //Decodifica el QR pasado como parametro y lo almacena en la variable textoQR
        textoQR = br.Decode(imagenQR).ToString();

        return textoQR;
    }
}