import {
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { apiRequest } from "@/api/httpClient";

export type Person = {
  userId: string;
  username: string;
  displayName: string;
  avatarUrl?: string;
  bio?: string;
};
export type Profile = Person & {
  visibility: string;
  isOwn: boolean;
  isRestricted: boolean;
  isFriend: boolean;
  isFollowing: boolean;
  incomingRequest: boolean;
  outgoingRequest: boolean;
  followers: number;
  following: number;
  friends: number;
  publicSets: number;
  publicCards: number;
  showStreak: boolean;
  showBattleHistory: boolean;
  showActivityStatus: boolean;
};
export type SocialSet = {
  id: string;
  title: string;
  description: string | null;
  author: Person;
  cardCount: number;
  likes: number;
  saves: number;
  learners: number;
  isLiked: boolean;
  isSaved: boolean;
  visibility: string;
  tags: string;
  publishedAt: string;
};
export type SocialCard = {
  id: string;
  frontText: string;
  backText: string;
  explanation?: string;
  readingText?: string;
  romanization?: string;
  exampleText?: string;
  exampleTranslation?: string;
  memoryTip?: string;
  hasImage: boolean;
  hasPrivateSource: boolean;
  likes: number;
  isLiked: boolean;
};
export type SetDetail = SocialSet & {
  isOwn: boolean;
  allowComments: boolean;
  allowRemix: boolean;
  cards: SocialCard[];
  attribution?: { title: string; author: string; studySetId?: string };
};
export type Comment = {
  id: string;
  parentCommentId?: string;
  content: string;
  isDeleted: boolean;
  isOwn: boolean;
  createdAt: string;
  author: Person;
};
export type Notice = {
  id: string;
  kind: string;
  entityId: string;
  isRead: boolean;
  createdAt: string;
  actor: Person;
};
export type Conversation = { id: string; person: Person; unread: number };
export type Message = {
  id: string;
  kind: string;
  content: string;
  isOwn: boolean;
  isRead: boolean;
  createdAt: string;
  shared?: { title: string; url: string };
};
export type Page<T> = {
  items: T[];
  hasMore: boolean;
  page: number;
  unread?: number;
};
export const socialGet = <T>(path: string) => apiRequest<T>(`/social${path}`);
export const socialSend = <T>(path: string, method = "POST", body?: unknown) =>
  apiRequest<T>(`/social${path}`, {
    method,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
export function useSocialList<T>(
  path: string,
  enabled = true,
  polling?: number,
) {
  return useInfiniteQuery({
    queryKey: ["social", path],
    queryFn: ({ pageParam }) =>
      socialGet<Page<T>>(
        `${path}${path.includes("?") ? "&" : "?"}page=${pageParam}`,
      ),
    initialPageParam: 1,
    getNextPageParam: (page) => (page.hasMore ? page.page + 1 : undefined),
    enabled,
    refetchInterval: polling,
    retry: false,
  });
}
export function useSocialAction<T = unknown>() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({
      path,
      method = "POST",
      body,
    }: {
      path: string;
      method?: string;
      body?: unknown;
    }) => socialSend<T>(path, method, body),
    onSuccess: () => {
      void cache.invalidateQueries({ queryKey: ["social"] });
    },
  });
}
export const visibilityLabels: Record<string, string> = {
  Private: "Chỉ mình tôi",
  FriendsOnly: "Bạn bè",
  Public: "Cộng đồng",
  Unlisted: "Người có liên kết",
};
