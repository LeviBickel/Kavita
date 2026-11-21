import {ScrobbleProvider} from "../_services/scrobbling.service";
import {AgeRating} from "./metadata/age-rating";
import {IHasCover} from "./common/i-has-cover";

export interface UserCollection extends IHasCover {
  id: number;
  title: string;
  promoted: boolean;
  coverImageLocked: boolean;
  summary: string;
  lastSyncUtc: string;
  owner: string;
  source: ScrobbleProvider;
  sourceUrl: string | null;
  totalSourceCount: number;
  /**
   * HTML anchors separated by <br/>
   */
  missingSeriesFromSource: string | null;
  ageRating: AgeRating;
  itemCount: number;
  /**
   * If set, this is a local Smart Collection: its series are kept in sync with whatever currently
   * matches this Smart Filter, rather than being manually managed.
   */
  smartFilterId: number | null;
  /**
   * Name of the backing Smart Filter, when smartFilterId is set.
   */
  smartFilterName: string | null;
}
