// tslint:disable-next-line
import MockItemSetData from '../mock/MockItemSetData';

declare global {
    interface Window {
        iagrim?: IAGrimHost;
        invokeCSharpAction?: (message: string) => void;
    }
}

export const isEmbedded = typeof window.invokeCSharpAction === 'function' && window.iagrim !== undefined;
// export const isEmbedded = window.iagrim !== undefined;
// export const isEmbedded = false;

export interface TransferResult {
    success: boolean;
}

interface IntegrationInterface {
    TransferItem(id: object[], transferAll: boolean): Promise<string>;

    SetClipboard(text: string): void;

    RequestMoreItems(): void;

    RequestCollectionData(): void;

    GetItemSetAssociations(): Promise<string>;

    GetBackedUpCharacters(): Promise<string>;
    GetCharacterDownloadUrl(character: string): Promise<string>;
    OpenURL(url: string): void;
    SignalReady(): void;

    GetTranslationStrings(): Promise<string>;

    DismissNumericFilterBanner(): void;
}


const core = window.iagrim;
// function getHost(): IAGrimHost | undefined {
//     return window.iagrim;
// }

export async function transferItem(url: object[], transferAll: boolean): Promise<TransferResult> {
  const id = url.join(';');
  if (isEmbedded) {
    const response = JSON.parse(await core.TransferItem(url, transferAll));
    return {success: response.success};
  } else {
    console.debug('Transfer Single', id);
    return {success: true};
  }
}

export function setClipboard(text: string): void {
  console.debug("Setting clipboard text");
  core.SetClipboard(text);
}

export function requestMoreItems(): void {
  if (isEmbedded) {
    console.debug("Requesting more items");
    core.RequestMoreItems();
  } else {
    console.debug('It wants itemsss doesss itssss? no more have it doessssss');
  }
}

// Ask C# to (re)build the Collection tab data. Only called when the Collection tab is open, since
// that heavy aggregate query is unrelated to normal item searching and most users never open it.
export function requestCollectionData(): void {
  if (isEmbedded) {
    console.debug("Requesting collection data");
    core.RequestCollectionData();
  }
}


let itemSetAssociationsCache: Promise<string> | undefined;
export async function getItemSetAssociations(): Promise<string> {
  if (isEmbedded) {
    if (itemSetAssociationsCache !== undefined)
      return await itemSetAssociationsCache;

    console.debug("Requesting item set associations");
    itemSetAssociationsCache = core.GetItemSetAssociations();
    return await itemSetAssociationsCache;
  }

  // Dev fixture. import.meta.env.DEV is replaced with a literal false when building for production, so the
  // branch and the ~60 KB of JSON behind it are dropped instead of being shipped and parsed on every launch.
  if (import.meta.env.DEV) {
    return Promise.resolve(JSON.stringify(MockItemSetData));
  }

  return '[]';
}


export function openUrl(url: string): void {
    core.OpenURL(url)
}

export function signalReady(): void {
    console.log("Notifying IAGD that we're ready")
    if (isEmbedded) {
        core.SignalReady()
    }
}

export function dismissNumericFilterBanner(): void {
  if (isEmbedded) {
    core.DismissNumericFilterBanner();
  } else {
    console.debug('Dismissing numeric filter banner');
  }
}

export interface CharacterListDto {
  name: string;
  createdAt: string;
  updatedAt: string;
}

export async function getBackedUpCharacters(): Promise<CharacterListDto[]> {
  if (isEmbedded) {
    return JSON.parse(await core.GetBackedUpCharacters());
  }

  return [{"name":"_Burn","createdAt":"2021-02-14T13:46:37.332325Z","updatedAt":"2021-02-15T16:41:28.098545Z"},{"name":"_Fog","createdAt":"2021-02-14T13:46:44.096884Z","updatedAt":"2021-02-15T16:41:28.543658Z"},{"name":"_HC Joe","createdAt":"2021-02-14T13:46:49.376661Z","updatedAt":"2021-02-15T16:41:28.929276Z"},{"name":"_Mist","createdAt":"2021-02-14T13:46:50.954797Z","updatedAt":"2021-02-15T16:41:29.559928Z"},{"name":"_Oaf","createdAt":"2021-02-14T13:46:52.057302Z","updatedAt":"2021-02-15T16:41:30.102216Z"},{"name":"_Ogor","createdAt":"2021-02-14T13:46:53.818669Z","updatedAt":"2021-02-15T16:41:30.530022Z"},{"name":"_Prison","createdAt":"2021-02-14T13:46:54.475951Z","updatedAt":"2021-02-15T16:41:31.139391Z"},{"name":"_Spirit","createdAt":"2021-02-14T13:46:54.878489Z","updatedAt":"2021-02-15T16:41:31.562969Z"},{"name":"_Stick","createdAt":"2021-02-14T13:46:55.375027Z","updatedAt":"2021-02-15T16:41:31.981983Z"},{"name":"_test","createdAt":"2021-02-14T13:46:55.735678Z","updatedAt":"2021-02-15T16:41:32.335665Z"},{"name":"_The Fireman","createdAt":"2021-02-14T13:46:56.116189Z","updatedAt":"2021-02-15T16:41:32.695291Z"},{"name":"_The Houndmaster of Yir","createdAt":"2021-02-14T13:46:56.534847Z","updatedAt":"2021-02-15T16:41:33.104993Z"},{"name":"_Tool","createdAt":"2021-02-14T13:46:56.915763Z","updatedAt":"2021-02-15T16:41:33.511978Z"},{"name":"_Worf","createdAt":"2021-02-14T13:46:57.458046Z","updatedAt":"2021-02-15T16:41:34.880836Z"},{"name":"_Xzipnkiron","createdAt":"2021-02-14T13:46:57.89902Z","updatedAt":"2021-02-15T16:41:36.204445Z"},{"name":"__Fog","createdAt":"2021-02-14T13:46:58.274755Z","updatedAt":"2021-02-15T16:41:36.616057Z"},{"name":"__HC Joe","createdAt":"2021-02-14T13:46:58.574766Z","updatedAt":"2021-02-15T16:41:37.059381Z"},{"name":"__test","createdAt":"2021-02-14T13:46:58.913527Z","updatedAt":"2021-02-15T16:41:37.449426Z"},{"name":"__Worf","createdAt":"2021-02-14T13:46:59.458262Z","updatedAt":"2021-02-15T16:41:37.903103Z"},{"name":"Joe","createdAt":"2021-02-15T15:50:29.190968Z","updatedAt":"2021-02-15T20:12:01.177349Z"}];
}


export interface CharacterUrlRequest {
  url: string|undefined;
}


export async function getCharacterDownloadUrl(character: string): Promise<CharacterUrlRequest> {
  if (isEmbedded) {
    return JSON.parse(await core.GetCharacterDownloadUrl(character));
  }
  return {'url': undefined};
}

export async function getTranslationStrings(): Promise<ReturnType<IntegrationInterface['GetTranslationStrings']>> {
  if (isEmbedded) {
    const d = await core.GetTranslationStrings();
    return typeof d === 'string' ? JSON.parse(d) : d;
  }
  return {};
}
