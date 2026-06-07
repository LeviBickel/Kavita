import {ScrobbleProvider} from "../../../_services/scrobbling.service";
import {ScrobbleProviderSettings} from "./scrobble-provider-settings";

export class UserScrobbleProvider {
  provider!: ScrobbleProvider;
  userName!: string;
  authenticationToken!: string;
  validUntilUtc!: string;
  lastSyncedUtc!: string;
  hasRunScrobbleEventGeneration!: boolean;
  scrobbleEventGenerationRan!: string;
  settings!: ScrobbleProviderSettings;

  get generateTokenLink(): string | null {
    switch (this.provider) {
      case ScrobbleProvider.AniList:
        return "https://anilist.co/api/v2/oauth/authorize?client_id=12809&redirect_url=https://anilist.co/api/v2/oauth/pin&response_type=token";
      case ScrobbleProvider.Hardcover:
        return "https://hardcover.app/account/api";
      case ScrobbleProvider.MangaBaka:
        return "https://mangabaka.org/my/settings/api-and-apps";
    }

    return null;
  }

  get canGenerateEvents(): boolean {
    if (this.provider === ScrobbleProvider.Mal) {
      return false;
    }

    return (this.authenticationToken ?? '') !== '';
  }



  static From(data: Partial<UserScrobbleProvider>): UserScrobbleProvider {
    return Object.assign(new UserScrobbleProvider(), data);
  }
}
