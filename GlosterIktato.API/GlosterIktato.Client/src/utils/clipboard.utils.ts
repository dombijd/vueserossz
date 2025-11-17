import { useToast } from '@/composables/useToast';

/**
 * Copy text to clipboard with toast notification
 * @param text - Text to copy to clipboard
 * @param successMessage - Optional success message (default: "Másolva a vágólapra")
 * @param errorMessage - Optional error message (default: "Nem sikerült másolni")
 */
export async function copyToClipboard(
  text: string,
  successMessage = 'Másolva a vágólapra',
  errorMessage = 'Nem sikerült másolni'
): Promise<void> {
  const { success, error } = useToast();

  try {
    await navigator.clipboard.writeText(text);
    success(successMessage);
  } catch (err) {
    error(errorMessage);
  }
}

